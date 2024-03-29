﻿//https://github.com/dotnet/roslyn-sdk/blob/0abb5881b483493b198315c83b4679b6a13a4545/LICENSE.txt
//https://github.com/dotnet/roslyn-sdk/blob/0abb5881b483493b198315c83b4679b6a13a4545/samples/CSharp/SourceGenerators/SourceGeneratorSamples/AutoNotifyGenerator.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AutoNotify.Generator
{
    [Generator]
    public class AutoNotifyGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {            
            //Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());

//#if DEBUG
//            if (!Debugger.IsAttached)
//            {
//                Debugger.Launch();
//            }
//#endif 
            //Debug.WriteLine("Initalize code generator");
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // add the attribute text
            //context.AddSource("AutoNotifyAttribute", SourceText.From(attributeText, Encoding.UTF8));

            // retreive the populated receiver 
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            // get the newly bound attribute, and INotifyPropertyChanged
            INamedTypeSymbol attributeSymbol = context.Compilation.GetTypeByMetadataName("AutoNotify.AutoNotifyAttribute");
            INamedTypeSymbol notifySymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged");

            // loop over the candidate fields, and keep the ones that are actually annotated
            List<IFieldSymbol> fieldSymbols = new List<IFieldSymbol>();
            foreach (FieldDeclarationSyntax field in receiver.CandidateFields)
            {
                SemanticModel model = context.Compilation.GetSemanticModel(field.SyntaxTree);
                foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                {
                    // Get the symbol being decleared by the field, and keep it if its annotated
                    IFieldSymbol fieldSymbol = model.GetDeclaredSymbol(variable) as IFieldSymbol;
                    if (fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default)))
                    {
                        fieldSymbols.Add(fieldSymbol);
                    }
                }
            }

            // group the fields by class, and generate the source
#pragma warning disable RS1024 // Symbols should be compared for equality. changing this causes errors.
            foreach (IGrouping<INamedTypeSymbol, IFieldSymbol> group in fieldSymbols.GroupBy(f => f.ContainingType))
            {
                string classSource = ProcessClass(group.Key, group.ToList(), attributeSymbol, notifySymbol, context);
                context.AddSource($"{group.Key.Name}_autoNotify.cs", SourceText.From(classSource, Encoding.UTF8));
            }
#pragma warning restore RS1024 // Symbols should be compared for equality
        }

        private string ProcessClass(INamedTypeSymbol classSymbol, List<IFieldSymbol> fields, ISymbol attributeSymbol, ISymbol notifySymbol, GeneratorExecutionContext context)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                return null; //TODO: issue a diagnostic that it must be top level
            }

            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            // begin building the generated source
            StringBuilder source = new StringBuilder($@"
namespace {namespaceName}
{{
    public partial class {classSymbol.Name} : {notifySymbol.ToDisplayString()}
    {{
        partial void OnPropertyChanged(string propertyName);");

            // if the class doesn't implement INotifyPropertyChanged already, add it
#pragma warning disable RS1024 // Symbols should be compared for equality. changing this causes errors.
            if (!classSymbol.Interfaces.Contains(notifySymbol))
            {
                source.Append(@"
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

");
            }
#pragma warning restore RS1024 // Symbols should be compared for equality

            // create properties for each field 
            foreach (IFieldSymbol fieldSymbol in fields)
            {
                ProcessField(source, fieldSymbol, attributeSymbol);
            }

            ProcessGetValue(source, fields, attributeSymbol);

            ProcessSetValue(source, fields, attributeSymbol);

            source.Append(@"
    } 
}");
            return source.ToString();
        }

        private void ProcessGetValue(StringBuilder source, List<IFieldSymbol> fields, ISymbol attributeSymbol)
        {
            source.Append(@"

        public object? GetValue(string propertyName)
        {
            switch (propertyName)
            {");
            //create getter
            foreach (IFieldSymbol fieldSymbol in fields)
            {
                string propertyName = GetPropertyName(fieldSymbol, attributeSymbol);

                if (propertyName != null)
                {
                    source.Append(@$"
                    case nameof({propertyName}):
                        return {propertyName};");
                }
            }
            source.Append(@"
                default:
                    throw new ArgumentException();
            }
        }");
        }

        private void ProcessSetValue(StringBuilder source, List<IFieldSymbol> fields, ISymbol attributeSymbol)
        {
            source.Append(@"

    public void SetValue(string propertyName, object? value)
    {
        switch (propertyName)
        {");
            //create getter
            foreach (IFieldSymbol fieldSymbol in fields)
            {
                string propertyName = GetPropertyName(fieldSymbol, attributeSymbol);

                if (propertyName != null)
                {
                    var nullable = fieldSymbol.NullableAnnotation == NullableAnnotation.Annotated;
                    source.Append(@$"
                case nameof({propertyName}):
                    if ({(nullable ? "value == null || ":"")}value is {(nullable ? fieldSymbol.Type.OriginalDefinition : fieldSymbol.Type)})
                        {propertyName} = ({fieldSymbol.Type})value;
                    else
                        throw new ArgumentException(""\""{propertyName}\"" only accepts values of type {fieldSymbol.Type}."");
                    break;");
                }
            }
            source.Append(@"
            default:
                throw new ArgumentException();
        }
    }");
        }

        private void ProcessField(StringBuilder source, IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
        {            
            string propertyName = GetPropertyName(fieldSymbol, attributeSymbol);
            if (propertyName != null)
            {
                // get the name and type of the field
                string fieldName = fieldSymbol.Name;
                ITypeSymbol fieldType = fieldSymbol.Type;

                source.Append($@"
    public {fieldType} {propertyName} 
    {{
        get 
        {{
            return this.{fieldName};
        }}

        set
        {{
            this.{fieldName} = value;
            this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof({propertyName})));
            OnPropertyChanged(nameof({propertyName}));
        }}
    }}

");
            }
            else
            {
                //TODO: issue a diagnostic that we can't process this field
            }
        }

        private string GetPropertyName(IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
        {
            string fieldName = fieldSymbol.Name;

            // get the AutoNotify attribute from the field, and any associated data
            AttributeData attributeData = fieldSymbol.GetAttributes().Single(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
            TypedConstant overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;

            string propertyName = ChooseName(fieldName, overridenNameOpt);
            if (propertyName.Length == 0 || propertyName == fieldName)
            {
                //TODO: issue a diagnostic that we can't process this field
                return null;
            }
            else
                return propertyName;
        }

        private string ChooseName(string fieldName, TypedConstant overridenNameOpt)
        {
            if (!overridenNameOpt.IsNull)
            {
                return overridenNameOpt.Value.ToString();
            }

            fieldName = fieldName.TrimStart('_');
            if (fieldName.Length == 0)
                return string.Empty;

            if (fieldName.Length == 1)
                return fieldName.ToUpper();

            return fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1);
        }


        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        class SyntaxReceiver : ISyntaxReceiver
        {
            public List<FieldDeclarationSyntax> CandidateFields { get; } = new List<FieldDeclarationSyntax>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // any field with at least one attribute is a candidate for property generation
                if (syntaxNode is FieldDeclarationSyntax fieldDeclarationSyntax
                    && fieldDeclarationSyntax.AttributeLists.Count > 0)
                {
                    CandidateFields.Add(fieldDeclarationSyntax);
                }
            }
        }
    }
}

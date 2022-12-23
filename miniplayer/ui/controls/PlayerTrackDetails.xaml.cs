using miniplayer.lib;
using miniplayer.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace miniplayer.ui.controls
{
    /// <summary>
    /// Interaction logic for PlayerTrackDetails.xaml
    /// </summary>
    public partial class PlayerTrackDetails : UserControl
    {
        #region NameCsv Attached Property

        internal static IEnumerable<LinkableObject> GetNameCsv(DependencyObject obj)
        {
            return (IEnumerable<LinkableObject>)obj.GetValue(NameCsvProperty);
        }

        internal static void SetNameCsv(DependencyObject obj, IEnumerable<LinkableObject> value)
        {
            obj.SetValue(NameCsvProperty, value);
        }

        // Using a DependencyProperty as the backing store for NameCsv.  This enables animation, styling, binding, etc...
        internal static readonly DependencyProperty NameCsvProperty =
            DependencyProperty.RegisterAttached(
                "NameCsv", 
                typeof(IEnumerable<LinkableObject>), 
                typeof(PlayerTrackDetails), 
                new PropertyMetadata(
                    Enumerable.Empty<LinkableObject>(), 
                    new PropertyChangedCallback(NameCsvPropertyChanged)));

        public static void NameCsvPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock tb)
            {
                tb.Inlines.Clear();

                if (e.NewValue is IEnumerable<LinkableObject> objs && objs.Any())
                {
                    var first = objs.First();
                    tb.Inlines.Add(new Run(first.Name) { Tag = first });
                    foreach(var o in objs.Skip(1))
                    {
                        tb.Inlines.Add(new Run(", "));
                        tb.Inlines.Add(new Run(o.Name) { Tag = o });
                    }
                }
            }
        }
        #endregion

        public PlayerTrackDetails()
        {
            InitializeComponent();            
        }

        private void _title_text_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.DataContext is PlayerModel model && model.CurrentlyPlaying != null)
                PlayerCommands.OpenInSpotify.Execute(model.CurrentlyPlaying, this);
        }

        private void _artist_text_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Run r && r.Tag != null)
                PlayerCommands.OpenInSpotify.Execute(r.Tag, this);
        }
    }
}

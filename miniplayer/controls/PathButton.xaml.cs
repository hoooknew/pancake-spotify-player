using System;
using System.Collections.Generic;
using System.Linq;
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

namespace miniplayer.controls
{
    /// <summary>
    /// Interaction logic for PathButton.xaml
    /// </summary>
    public partial class PathButton : Button
    {
        public Geometry PathData
        {
            get { return (Geometry)GetValue(PathDataProperty); }
            set { SetValue(PathDataProperty, value); }
        }
        
        public static readonly DependencyProperty PathDataProperty =
            DependencyProperty.Register("PathData", typeof(Geometry), typeof(PathButton), new PropertyMetadata(Geometry.Empty));

        public Brush PathFill
        {
            get { return (Brush)GetValue(PathFillProperty); }
            set { SetValue(PathFillProperty, value); }
        }

        public static readonly DependencyProperty PathFillProperty =
            DependencyProperty.Register("PathFill", typeof(Brush), typeof(PathButton), new PropertyMetadata(Brushes.DodgerBlue));



        public Brush PathStroke
        {
            get { return (Brush)GetValue(PathStrokeProperty); }
            set { SetValue(PathStrokeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PathStroke.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PathStrokeProperty =
            DependencyProperty.Register("PathStroke", typeof(Brush), typeof(PathButton), new PropertyMetadata(Brushes.Transparent));


        public double PathStrokeThickness
        {
            get { return (double)GetValue(PathStrokeThicknessProperty); }
            set { SetValue(PathStrokeThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PathStrokeThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PathStrokeThicknessProperty =
            DependencyProperty.Register("PathStrokeThickness", typeof(double), typeof(PathButton), new PropertyMetadata(1.0));




        public PathButton()
        {
            InitializeComponent();   
        }
    }
}

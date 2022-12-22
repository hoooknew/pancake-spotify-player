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

namespace miniplayer.ui.controls
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


        public PathButton()
        {
            this.DefaultStyleKey = typeof(PathButton);
        }
    }
}

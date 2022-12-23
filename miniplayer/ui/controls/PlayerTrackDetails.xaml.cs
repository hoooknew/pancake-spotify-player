using miniplayer.models;
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
    /// Interaction logic for PlayerTrackDetails.xaml
    /// </summary>
    public partial class PlayerTrackDetails : UserControl
    {
        public PlayerTrackDetails()
        {
            InitializeComponent();
        }

        private void _title_text_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.DataContext is PlayerModel model)
            {
                PlayerCommands.OpenInSpotify.Execute(null, this);
            }
        }

        private void _artist_text_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.DataContext is PlayerModel model)
            {
                PlayerCommands.OpenInSpotify.Execute(null, this);
            }
        }
    }
}

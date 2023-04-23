using pancake.lib;
using pancake.models;
using pancake.ui.controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace pancake
{
    /// <summary>
    /// Interaction logic for PlaylistWindow.xaml
    /// </summary>
    public partial class PlaylistWindow : BaseWindow
    {
        public const double PLAYING_HEIGHT = 118;
        public const double QUEUED_HEIGHT = 94;
        private readonly IPlaylistModel _playlistModel;
        public PlaylistWindow(IPlaylistModel model)
        {
            InitializeComponent();

            _playlistModel = model;
            this.DataContext = model;
            this.SizeChanged += PlaylistWindow_SizeChanged;

            SetQueuedLength();
        }

        private void PlaylistWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetQueuedLength();
        }

        private void SetQueuedLength()
        {
            var availableWidth = _root.ActualWidth - (_stackPanel.ActualWidth - _queued.ActualWidth);
            
            double minWidth = QUEUED_HEIGHT * Settings.Instance.UiScale;
            double itemWidth;
            if (_playlistModel.Queued.Count > 0)
                itemWidth = Math.Max(minWidth, _queued.ActualWidth / _playlistModel.Queued.Count);
            else
                itemWidth = minWidth;

            _playlistModel.QueuedLength = Math.Max(4, (int)Math.Ceiling(availableWidth / itemWidth));
        }
    }
}

using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miniplayer.models
{
    internal class ContextModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ContextModel() { }
        
        private CurrentlyPlayingContext? _context;        

        public void SetContext(CurrentlyPlayingContext? context)
        {
            this._context = context;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
        }

        private FullTrack _ft;
        /*
         * FullTrack
         * List<SimpleArtist> Artists 
         * SimpleAlbum Album
         */
        private FullEpisode _fe;
        /*
         * FullEpisode
         * string Name
         * SimpleShow Show
         * Images
         * Description
         */
    }
}

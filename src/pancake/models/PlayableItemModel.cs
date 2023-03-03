using pancake.lib;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace pancake.models
{
    public class PlayableItemModel
    {
        private IPlayableItem _item;
        public PlayableItemModel(IPlayableItem item) 
        { 
            _item = item;
        }

        public string Id
            => _item switch
            {
                FullTrack track => track.Id,
                FullEpisode ep => ep.Id,
                _ => throw new ArgumentException()
            };

        public string Title
            => _item switch 
            { 
                FullTrack track => track.Name, 
                FullEpisode ep => ep.Name,
                _ => throw new ArgumentException()
            };

        public IEnumerable<LinkableObject> Artists            
            => _item switch 
            { 
                FullTrack track => track.Artists.Select(r => (LinkableObject)r), 
                FullEpisode ep => new[] { (LinkableObject)ep.Show },
                _ => throw new ArgumentException()
            };

        private Image PickImage(IEnumerable<Image> images)
            => images.Where(r => r.Width <= 300).OrderByDescending(r => r.Width).FirstOrDefault() ?? images.First();


        public Image Image
            => _item switch
            {
                FullTrack track => PickImage(track.Album.Images),
                FullEpisode ep => PickImage(ep.Images),
                _ => throw new ArgumentException()
            };
    }
}

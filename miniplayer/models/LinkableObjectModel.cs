using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miniplayer.models
{
    internal record LinkableObjectModel(string Name, string Href, string Uri, string Type)
    {
        public static implicit operator LinkableObjectModel(SimpleArtist a) =>
            new LinkableObjectModel(a.Name, a.Href, a.Uri, a.Type);

        public static implicit operator LinkableObjectModel(SimpleShow s) =>
            new LinkableObjectModel(s.Name, s.Href, s.Uri, s.Type);
    }
}

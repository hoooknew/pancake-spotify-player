using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pancake.lib
{
    public record LinkableObject(string Name, string Type, string Id, string Uri)
    {
        public static implicit operator LinkableObject(SimpleArtist a) =>
            new LinkableObject(a.Name, a.Type, a.Id, a.Uri);

        public static implicit operator LinkableObject(SimpleShow s) =>
            new LinkableObject(s.Name, s.Type, s.Id, s.Uri);
    }
}

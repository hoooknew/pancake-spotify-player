using Moq;
using pancake.models;
using pancake.spotify;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pancake.tests.models
{
    public class PlayerModelTests
    {
        [Fact]
        public void Test1()
        {
            //times pauses when the pause state is recieved
            //the progress is set to the one from the state when paused.
            //the favorite it loaded when a different song is loaded
        }

        private static CurrentlyPlayingContext Create(Action<CurrentlyPlayingContext> edit)
        {
            var cpc = new CurrentlyPlayingContext()
            {
                IsPlaying = false,
                ShuffleState = false,
                RepeatState = "off",
                ProgressMs = 0,
                Item = new FullTrack()
                {
                    Id = "id",
                    Name = "Track Name",
                    Artists = new List<SimpleArtist>()
                    {
                        new SimpleArtist(){ Name = "Artist" }
                    },
                    DurationMs = 60 * 1_000

                }
            };
            edit(cpc);

            return cpc;
        }

        [Fact]
        public void time_stops_when_paused()
        {
            //times pauses when the pause state is recieved
            var client = new Mock<ISpotifyClient>();
            client
                .SetupSequence(r => r.Player.GetCurrentPlayback(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Create(cpc =>
                    {
                        cpc.IsPlaying = true;
                        cpc.ProgressMs = 5 * 1_000;
                    }))
                .ReturnsAsync(Create(cpc =>
                {
                    cpc.IsPlaying = false;
                    cpc.ProgressMs = 8 * 1_000;
                }));

            client
                .Setup(r => r.Library.CheckTracks(It.IsAny<LibraryCheckTracksRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<bool>() { false });



            var token = new Mock<IRefreshableToken>();
            var factory = new Mock<IClientFactory>();
            factory.Setup(r => r.CreateClient(token.Object)).Returns(client.Object);

            var model = new PlayerModel(factory.Object);
        }
    }
}

using Moq;
using pancake.lib;
using pancake.models;
using pancake.spotify;
using pancake.tests.lib;
using pancake.ui.controls;
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
        private static CurrentlyPlayingContext PlayingContext(Action<CurrentlyPlayingContext> edit)
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

        private static Mock<IClientFactory> ClientFactory(Action<Mock<ISpotifyClient>> edit)
        {
            var client = new Mock<ISpotifyClient>();

            edit(client);

            var factory = new Mock<IClientFactory>();
            factory.Setup(r => r.CreateClient(It.IsAny<object>())).Returns(client.Object);

            return factory;
        }
        [Fact]
        public void time_is_stopped_when_always_paused()
        {
            var final = PlayingContext(cpc =>
            {
                cpc.IsPlaying = false;
                cpc.ProgressMs = 5 * 1_000;
            });

            var factory = ClientFactory(client =>
            {
                client
                    .SetupSequence(r => r.Player.GetCurrentPlayback(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(final)
                    .ReturnsAsync(final)
                    .ReturnsAsync(final)
                    .ReturnsAsync(final)
                    .ReturnsAsync(final)
                    .ReturnsAsync(final);

                client
                    .Setup(r => r.Library.CheckTracks(It.IsAny<LibraryCheckTracksRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<bool>() { false });
            });


            var config = new Mock<IConfig>();
            config.Setup(r => r.RefreshDelayMS).Returns(2000);


            var model = new PlayerModel(config.Object, factory.Object, new DebugLogging());

            model.SetToken(new object());
            Thread.Sleep(5_500);
            model.Dispose();
            Assert.True(!model.IsPlaying && model.Position == final.ProgressMs);
        }

        [Fact]
        public void time_stops_when_paused()
        {
            var final = PlayingContext(cpc =>
            {
                cpc.IsPlaying = false;
                cpc.ProgressMs = 11 * 1_000;
            });

            var factory = ClientFactory(client =>
            {
                client
                    .SetupSequence(r => r.Player.GetCurrentPlayback(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(PlayingContext(cpc =>
                    {
                        cpc.IsPlaying = true;
                        cpc.ProgressMs = 5 * 1_000;
                    }))
                    .ReturnsAsync(final)
                    .ReturnsAsync(final)
                    .ReturnsAsync(final)
                    .ReturnsAsync(final);

                client
                    .Setup(r => r.Library.CheckTracks(It.IsAny<LibraryCheckTracksRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<bool>() { false });
            });


            var config = new Mock<IConfig>();
            config.Setup(r => r.RefreshDelayMS).Returns(2000);


            var model = new PlayerModel(config.Object, factory.Object, new DebugLogging());

            model.SetToken(new object());
            Thread.Sleep(5_500);
            model.Dispose();
            Assert.True(!model.IsPlaying && model.Position == final.ProgressMs);            
        }
    }
}

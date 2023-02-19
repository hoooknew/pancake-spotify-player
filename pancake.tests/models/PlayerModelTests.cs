using Moq;
using pancake.lib;
using pancake.models;
using pancake.spotify;
using pancake.tests.lib;
using pancake.tests.spotify;
using pancake.ui.controls;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            factory.Setup(r => r.CreateClient()).Returns(client.Object);
            factory.Setup(r => r.HasToken).Returns(true);

            return factory;
        }

        private static Mock<IClientFactory> ClientFactory(SpotifyClientFake fake)
        {
            var factory = new Mock<IClientFactory>();
            factory.Setup(r => r.CreateClient()).Returns(fake);

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
            factory.Raise(cf => cf.TokenChanged += null, EventArgs.Empty);

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
            factory.Raise(cf => cf.TokenChanged += null, EventArgs.Empty);

            Thread.Sleep(5_500);
            model.Dispose();
            Assert.True(!model.IsPlaying && model.Position == final.ProgressMs);            
        }

        //I'm unconvinced that this test does anything useful.
        [Fact]
        public async void tick_delayed_after_play()
        {
            int refresh_delay = 3_000;
            int starting_ms = 0_000;            

            var config = new Mock<IConfig>();
            config.Setup(r => r.RefreshDelayMS).Returns(refresh_delay);

            bool isPlaying = false;            
            Stopwatch time_playing = new Stopwatch();

            var client = new SpotifyClientFake();
            client.PlayerFake.Callback_ResumePlayback = request =>
                {
                    Debug.WriteLine($"set isplaying");
                    isPlaying = true;
                    time_playing.Start();
                    return true;
                };
            client.PlayerFake.Callback_GetCurrentPlayback = () => PlayingContext(cpc =>
                {
                    cpc.IsPlaying = isPlaying;
                    cpc.ProgressMs = starting_ms + (int)time_playing.ElapsedMilliseconds;                    

                    Debug.WriteLine($"isplaying:{isPlaying}");
                });

            var factory = ClientFactory(client);

            var model = new PlayerModel(config.Object, factory.Object, new DebugLogging());
            factory.Raise(cf => cf.TokenChanged += null, EventArgs.Empty);

            await Task.Delay(1000);
            await model.PlayPause();
            await Task.Delay(10_000);


            time_playing.Stop();
            model.Dispose();
            

            var time_passed = starting_ms + time_playing.ElapsedMilliseconds;
            Assert.True(model.IsPlaying && Math.Abs(model.Position - time_passed) < 1000);
            
        }
    }
}


## TODO

- [ ] delete the tokens on auth or api error
- [ ] disable the buttons while waiting on a state refresh from the server
- [ ] add an *accurate* internal timer to update the track position so we can lengthen the time between status polls.
- [ ] move the poll delay into the config
- [ ] refactor the main window into user controls to improve readability.
- [ ] use routed commands instead of click events
- [ ] add theming and a dark theme
  - [ ] sync theme to the windows default
- [ ] customize the path button so it changes the button path color instead of using the default button behavior.
- [ ] add a popover with extra track details, and album art.
- [ ] add links to the popover that open the artist, song, or album in the desktop client
- [ ] include at least part of the current playlist in the popover
- [ ] add a settings right click that creates a user settings json in the app data folder
  - [ ] add a settings feature to remember the last app position
    - [ ] remember to only restore the position if the window is in the current screen dimensions.
  - [ ] add a settings feature to keep the app always on top
  - [ ] have the option of responding into the windows media?
- [ ] try to reduce bandwidth by using `GetCurrentlyPlaying` more often than `GetCurrentPlayback`.


## References
[Getting a Client Id](https://support.heateor.com/get-spotify-client-id-client-secret/)
[Getting an access and refresh token](https://github.com/JohnnyCrazy/SpotifyAPI-NET/blob/master/SpotifyAPI.Web.Examples/Example.TokenSwap/Client/Program.cs)
[Getting Started with SpotifyAPI-NET](https://johnnycrazy.github.io/SpotifyAPI-NET/docs/getting_started)
https://johnnycrazy.github.io/SpotifyAPI-NET/docs/pkce
[PKCE console example](https://github.com/JohnnyCrazy/SpotifyAPI-NET/blob/54f8f8960fbd859781fd971efaca94462ca52468/SpotifyAPI.Web.Examples/Example.CLI.PersistentConfig/Program.cs)
[playback api](https://developer.spotify.com/documentation/web-api/reference/#/operations/get-information-about-the-users-current-playback)

https://github.com/JohnnyCrazy/SpotifyAPI-NET/blob/master/SpotifyAPI.Web/Models/Response/FullEpisode.cs
https://github.com/JohnnyCrazy/SpotifyAPI-NET/blob/master/SpotifyAPI.Web/Models/Response/FullTrack.cs

https://github.com/dotnet/wpf/blob/89d172db0b7a192de720c6cfba5e28a1e7d46123/src/Microsoft.DotNet.Wpf/src/Themes/XAML/Window.xaml

### checking and changing favorites
https://developer.spotify.com/documentation/web-api/reference/#/operations/check-users-saved-tracks
https://developer.spotify.com/documentation/web-api/reference/#/operations/save-tracks-user
https://developer.spotify.com/documentation/web-api/reference/#/operations/remove-tracks-user
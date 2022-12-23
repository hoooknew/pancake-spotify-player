
## TODO

- [x] delete the tokens on auth or api error

- [x] add an *accurate* internal timer to update the track position so we can lengthen the time between status polls.
- [x] move the poll delay into the config


- [x] refactor the main window into user controls to improve readability.

- [x] use routed commands instead of click events
  - [x] disable the buttons while waiting on a state refresh from the server
  - [x] get each command to retry a status refresh if the expected state doesn't return


- [x] handle `APITooManyRequestsException`
- [x] add theming and a dark theme
  - [ ] add option to sync theme to the windows default
- [x] reduce requested permission scope to a minimum.


- [x] customize the path button so it changes the button path color instead of using the default button behavior.
- [ ] add a popover with extra track details, and album art.
- [ ] add links to the popover that open the artist, song, or album in the desktop client
- [ ] include at least part of the current playlist in the popover


- [x] add a settings right click that creates a user settings json in the app data folder
  - [x] add a settings feature to remember the last app position
    - [x] remember to only restore the position if the window is in the current screen dimensions.
  - [x] add a settings feature to keep the app always on top
  - [ ] have the option of responding into the windows media keys?
  - [x] add a setting to zoom the whole window.
  - [x] option to show/hide progress bar
  - [x] option to show/hide controls
  - [x] option to sign out
  - [x] add a setting top hide the window in the task bar


- [ ] if there isn't a spotify client open, show a UI that indicates the issue
- [ ] offer a button to launch the desktop client?


- [ ] built an installer
  - [ ] figure out a versioning pattern
- [ ] create a github actions workflow to create releases
- [ ] make a readme that explains the app functionality
  - [ ] is there anything inside worth explaining how it works?
- [ ] style the auth callback page?

- [ ] expose errors in the login to the user.

- [ ] add the ability to skip to a new position in the track by clicking on the progress bar.

- [ ] Put an icon on the window.

# Bugs
- [ ] there is a meaningful delay between when a state change in requested and when the changed state is available in the server. It tends to be less than 250ms. I think I need to verify the expected state change took place and retry otherwise.
- [ ] retry on http errors

## times to update the state

### full refresh
- we know the song ends
- we see the song changed
- after the user clicks a button?

### small refresh 
as time ticks


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

### theme xaml (the xaml in the docs is wrong)
https://github.com/dotnet/wpf/blob/89d172db0b7a192de720c6cfba5e28a1e7d46123/src/Microsoft.DotNet.Wpf/src/Themes/XAML/ContextMenu.xaml
https://www.manuelmeyer.net/2014/08/wpf-the-simplest-way-to-get-the-default-template-of-a-control-as-xaml/


### checking and changing favorites
https://developer.spotify.com/documentation/web-api/reference/#/operations/check-users-saved-tracks
https://developer.spotify.com/documentation/web-api/reference/#/operations/save-tracks-user
https://developer.spotify.com/documentation/web-api/reference/#/operations/remove-tracks-user

## Source Generators
https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview
https://github.com/dotnet/roslyn-sdk/tree/main/samples/CSharp/SourceGenerators
https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md

### finding classes with an attribute in the source generator
https://andrewlock.net/using-source-generators-with-a-custom-attribute--to-generate-a-nav-component-in-a-blazor-app/

The functionality I want as a sample
https://github.com/dotnet/roslyn-sdk/blob/0abb5881b483493b198315c83b4679b6a13a4545/samples/CSharp/SourceGenerators/SourceGeneratorSamples/AutoNotifyGenerator.cs

### Theming
https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/button-styles-and-templates?view=netframeworkdesktop-4.8
https://medium.com/southworks/handling-dark-light-modes-in-wpf-3f89c8a4f2db
https://www.pinvoke.net/default.aspx/uxtheme.getcurrentthemename
https://engy.us/blog/2018/10/20/dark-theme-in-wpf/

https://github.com/AaronAmberman/WPF.Themes
https://github.com/vb2ae/WPFLightDarkMode

## Linking to spotify content
https://developer.spotify.com/documentation/general/guides/content-linking-guide/
https://medium.com/swlh/custom-protocol-handling-how-to-8ac41ff651eb
`HKEY_CLASSES_ROOT\spotify\shell\open\command`
`https://open.spotify.com/`
`spotify://`
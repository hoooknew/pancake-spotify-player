
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


- [x] if there isn't a spotify client open, show a UI that indicates the issue
- [x] offer a button to launch the desktop client?


- [x] built an installer
  - [ ] figure out a versioning pattern
- [ ] create a github actions workflow to create releases
- [ ] make a readme that explains the app functionality
  - [ ] is there anything inside worth explaining how it works?
- [x] style the auth callback page?

- [ ] expose errors in the login to the user.

- [ ] add the ability to skip to a new position in the track by clicking on the progress bar.

- [x] Put an icon on the window.
- [ ] Put the track picture in the background or make a larger layout that includes the full image
- [ ] Can I start spotify locally and play a playlist? How can I make sure the client is ready to play before I 

- [ ] change the dark theme to match the invert of the light theme
- [ ] offer some other accent color choices under themes
- [ ] replace the context menu settings with a dialog
  - [ ] add some hotkeys for some settings?

# Bugs
- [x] change the artist and title clicks to mouse up so the drag to move can work on top of them.
- [x] the clickable area for the title box includes the black space to the right.
- [x] the border on the bottom is missing at 125% zoom.
- [x] close button is missing on the sign in screen
- [x] rename app on the spotify dev site
- [x] fix the blurriness on high dpi displays when there is also a lower dpi display

## times to update the state

### full refresh
- we know the song ends
- we see the song changed
- after the user clicks a button?

### small refresh 
as time ticks


## References
[Getting a Client Id](https://support.heateor.com/get-spotify-client-id-client-secret/)
https://developer.spotify.com/dashboard/applications
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

## Building in Actions
https://github.com/microsoft/github-actions-for-desktop-apps/blob/lance/net6-update/.github/workflows/ci-net6-temp.yml
https://github.com/microsoft/github-actions-for-desktop-apps/blob/lance/net6-update/.github/workflows/cd-net6-temp.yml

## Distributing with MSIX
https://montemagno.com/distributing-a-net-core-3-wpf-and-winforms-app-with-msix/
Wix3
https://wixtoolset.org/docs/v3/msbuild/authoring_first_msbuild_project/

## MSI's with Advanced Installer
The free version is enough, but it unfortunately won't install [.net 6](https://download.visualstudio.microsoft.com/download/pr/0a672516-37fb-40de-8bef-725975e0b137/a632cde8d629f9ba9c12196f7e7660db/dotnet-sdk-6.0.404-win-x64.exe
) automatically.

## build the app with a product version
https://stackoverflow.com/questions/58433665/how-to-specify-the-assembly-version-for-a-net-core-project
`dotnet build /p:Version=1.2.3`

https://www.advancedinstaller.com/user-guide/set-version.html
```
$AI="C:\Program Files (x86)\Caphyon\Advanced Installer 20.2\bin\x86\AdvancedInstaller.com"
& $AI /edit "./installer/installer.aip" /SetVersion 1.2.3
```

https://github.com/marketplace/actions/advanced-installer-tool

## LOGO
https://redketchup.io/icon-editor
https://affinity.serif.com/en-us/designer/

## WPF DPI Awareness
https://github.com/Microsoft/WPF-Samples/tree/master/PerMonitorDPI
https://learn.microsoft.com/en-us/answers/questions/643007/how-to-set-permonitor-dpiawareness-for-net-6-windo.html
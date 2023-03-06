
## TODO

- [x] make a readme that describes the app, the features, and how to install it.
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
- [ ] create another window that is dockable to the top of the main window that has a horizontal stack of the albums in the playlist
    - [ ] it should show past, current, and future
    - [ ] should clicking on the items do anything? maybe change tracks or open the track details in spotify?


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
  - [x] figure out a versioning pattern
- [x] create a github actions workflow to create releases
    - [ ] change the CD so it triggers on the push of a release tag instead of a dispatch that makes the tag.
- [x] make a readme that explains the app functionality
  - [ ] is there anything inside worth explaining how it works?
- [x] style the auth callback page?

- [ ] expose errors in the login to the user.

- [ ] add the ability to skip to a new position in the track by clicking on the progress bar.

- [x] Put an icon on the window.
- [ ] Put the track picture in the background or make a larger layout that includes the full image
- [ ] Can I start spotify locally and play a playlist? How can I make sure the client is ready to play before I 

- [x] change the dark theme to match the invert of the light theme
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
- [x] the open spotify screen is taller than the normal screen. let's make it so the button shows up over the controls (or where they should be).
- [ ] prevent multiple copies of the app from opening?
- [ ] can I prevent the downloaded msi from being blocked my windows for free?
- [ ] handle the bad gateway error without signing the user out?
- [x] the queue in the playlist model crashes because I'm updating the list contents on a background thread.
- [ ] docked windows don't stay attached on high dpi displays. it looks like i need to use the dpi as a divisor.
- [ ] the get Queue call freaks out when the client is playing from an auto gen playlist that happens after a normal playlist ends.
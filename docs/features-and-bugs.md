
## TODO

- [x] add theming and a dark theme
  - [ ] add option to sync theme to the windows default


- [x] customize the path button so it changes the button path color instead of using the default button behavior.
- [x] create another window that is dockable to the top of the main window that has a horizontal stack of the albums in the playlist
    - [ ] it should show past, current, and future
    - [ ] should clicking on the items do anything? maybe change tracks or open the track details in spotify?


- [x] add a settings right click that creates a user settings json in the app data folder
  - [ ] have the option of responding into the windows media keys?
  

- [x] create a github actions workflow to create releases
    - [ ] change the CD so it triggers on the push of a release tag instead of a dispatch that makes the tag.
- [x] make a readme that explains the app functionality
  - [ ] is there anything inside worth explaining how it works?

- [ ] expose errors in the login to the user.

- [ ] add the ability to skip to a new position in the track by clicking on the progress bar.

- [ ] Can I start spotify locally and play a playlist? How can I make sure the client is ready to play before I 

- [ ] offer some other accent color choices under themes
- [ ] replace the context menu settings with a dialog
  - [ ] add some hotkeys for some settings?

- [x] create an option to hide and show the playlist
- [ ] increase the number of playlist pictures showing when it's made wider
- [x] have the location of the playlist window save independently.
- [ ] increased the amount of logging to identify and fix the startup token crash
- [ ] reposition docked windows after zoom.

# Bugs
- [ ] prevent multiple copies of the app from opening?
- [ ] can I prevent the downloaded msi from being blocked my windows for free?
- [ ] handle the bad gateway error without signing the user out?
- [x] the queue in the playlist model crashes because I'm updating the list contents on a background thread.
- [x] docked windows don't stay attached on high dpi displays. it looks like i need to use the dpi as a divisor.
- [ ] the get Queue call freaks out when the client is playing from an auto gen playlist that happens after a normal playlist ends.
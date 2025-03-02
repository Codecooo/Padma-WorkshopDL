<p align="center">
  <img src="https://i.imgur.com/skYvswx.png" />
</p>

# Padma Workshop Downloader
Padma Workshop Downloader is a GUI wrapper for steamcmd to download workshop mods in Linux with easy to use and user friendly.

## Features of Padma
| Features | Status |
|----------|----------|
| Install 1 GB+ mods    | <p align="center"> :white_check_mark: </p> |
| Auto install Stellaris mods    | <p align="center"> :white_check_mark: </p> |
| Change download directory   | <p align="center"> :white_check_mark: </p> |
| Download history   | <p align="center"> :white_check_mark: </p>|
| Auto fetch AppID   | <p align="center"> :white_check_mark: </p> |

With Padma now for Stellaris mods you don't have to manually install them with Paradox Launcher, the app will do that for you. It will automatically install Stellaris mods once they detect it. Of course you could disable this in settings.

## How to Use
1. Install the AppImage file from Releases in this github page
2. Mark the AppImage file as executable by running this command in terminal `chmod +x Padma.AppImage`or set it as executable through file manager GUI. Run the AppImage file
3. If you want to check for supported games, head over to the Supported Games tab (console icon) and search for your games
4. In the home page, enter the steam workshop URL, wait for the process to fetch appID finish and then click download.
5. For first use it will be quite slow because it will update steamcmd but once it finishes the process should be faster.

## Supported Games
The supported games for now is on this list. If you want to check for supported games in the app, head over to the Supported Games tab (console icon) and search for your games
<br>
[Supported Games List](https://steamdb.info/sub/17906/apps/)

## Troubleshooting
<details>
<summary> <strong>My mod download stuck when trying to download +500 MB (large size)</strong></summary>
  <br>
  Generally speaking, when the status in download bar is still "Downloading" it means the download is stil not failed. If you're downloading a large mod it will take awhile since there is timeouts between downloads and need to be resumed. But don't worry just wait the download to finish
</details>
<details>
<summary> <strong>My download result in "Output: Download Failure" with exit code 0</strong> </summary>
<br>
  This is a known issue if the steamapps folder is corrupted either by moving downloaded mods or something else.
  <br>
  Go to Settings and press "Reset Padma", this will reset Padma including all of it's data to ensure proper fix.
  </details>
  <details>
<summary> <strong>Other problems</strong> </summary> 
<br>
You could open an issue in this github page and the describe what the issue, what do you do before, and if you can copy the logs in the console
</details>


## Future Plans
1. Cross Platform (Mac and Windows)
2. Multiple downloads
3. Collections
4. Support for other providers (quite hard)

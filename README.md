Safeguard
=========

Savegame backup helper for Timber and Stone.

How to use
==========

Just copy the two files from the Zip in your Timber and Stone folder and run Safeguard.exe instead of the 'Timber and Stone.exe' to start the game.
Safeguard will recognize if Timber and Stone saves a settlement to the disk and will copy the save files to the Safeguard subfolder.
Safeguard will automatically close itself when Timber and Stone is closed.

Per default, up to 10 backups per settlement are kept in the Safeguard subfolder. Safeguard will maintain this number by deleting the oldest backups when it is beeing closed.
Also there is a minimum time span between backups of 5 minutes.
Expirienced users may take a look in the .config file to change these default values.

To restore one of the backups you have to copy the *.sav files back to the original savegame folder. Feel free to ask for a detailed explanation, if needed.

Requirements
============

.Net Framework 2.0 or higher.

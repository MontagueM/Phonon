# Phonon

## What is this?

A GUI with similar functionality of the console application [MDE](https://github.com/MontagueM/MontevenDynamicExtractor).

Current capability:

* Works with packages for Beyond Light and before Beyond Light
* Scrolling through all dynamic models in the entire of Destiny 2
* Exporting with option for name, folder, and texture extraction

Download and have a look, it should be much easier to use than MDE.

## How do I install and use it?

Do not run Destiny 2 at the same time as booting Phonon for the first time. 

You won't get banned but if they try to read the same package Phonon will not like it.

* Download the [latest release](https://github.com/MontagueM/Phonon/releases/latest/) and run Phonon. 
* You'll need [.NET 5.0 x64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-5.0.12-windows-x64-installer) installed but most people have that.
* Once it's open select your Destiny 2/packages/ directory. It will tell you if it's wrong.
* Phonon will freeze for a bit after that to generate content manifest which is a one-time-per-update thing.
* After some time it should populate the left bar with a list of packages which can be selected to show their dynamics.
* Exporting can be done by using the Export button on the menu bar.

## How do I use it for before Beyond Light?

Find a file called Phonon.exe.config or similar depending on the name of the executable. If the file does not exist make sure to run the program at least once before so it opens. You can then close it and open the config file, changing the key "Value" from "Destiny2BL" to "Destiny2PREBL".

## How do I get the old packages?

Guide from nblock:

* You need to acquire .NET Core from Microsoft's website, make sure to download the correct version for your operating system (Windows x64 for most users, but please check this).
* Download the DepotDownloader tool from github (https://github.com/SteamRE/DepotDownloader/releases) and extract it somewhere you'll remember.
* Open the folder containing "DepotDownloader.dll", type "cmd.exe" into the address bar and press enter to launch command line from that location.
* Now you can enter the commands below to download the downpatched files for whichever Destiny version you need. You can get the appid, depotid and manifest # from a site like https://steamdb.info/, as well as replacing <username> with your steam account's details. It'll ask for your password and if you have Steam Guard active you will be prompted for the code like usual.

  dotnet DepotDownloader.dll -app <#> -depot <#> -manifest <id> -username <user>

 The recommended app and depot is app=1085661, depot=589374386951979820. This equates to the game version 2.9.2.2.

## Destiny 1

To use the Destiny 1 version, open the config file and change the key "Value" from "Destiny2BL"/"Destiny2PREBL" to "Destiny1". To access the packages, please ask in the #ripping chat of the Destiny Model Rips (DMR) server or DM me.

Thanks to Philip for help with textures swizzling for PS4.

## Errors

I'll list any common errors that appear here and how to fix them. Please raise any new issues in the Issues tab.

For now the safest thing to do if you encounter any error is to delete the whole program and redownload.

## What does Phonon mean

I wrote Phonon in a couple days procrastinating my solid state physics lectures, and the last thing I covered was phonons. 

It's basically just a term for vibrating atoms, and yes I had to google that.

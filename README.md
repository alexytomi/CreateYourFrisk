# Create Your Frisk - Android

This is a project porting [Create Your Frisk](https://github.com/RhenaudTheLukark/CreateYourFrisk) to Android
All issues with the port should be sent to this repository. Please DO NOT send it to the main repository.

Note: This branch is used to quickly and easily test Android-native code by using Unity's export project feature. This is not used to create proper builds.

***

## Changes

Files are now stored in /storage/emulated/0/Android/data/unity.NobodysGettingPaidHere.CreateYourFrisk/files
The intended way to access this is using any SAF compatible file explorer. (My personal suggestion is [Files by Marc](https://play.google.com/store/apps/details?id=com.marc.files), you can easily find the Create Your Frisk folder in the Sidebar!)

## Licenses
This project is licensed under GPLv3. The license can be viewed [here](LICENSE).


### External Code Used

[Termux](https://github.com/termux) - [TermuxDocumentsProvider](https://github.com/termux/termux-app/blob/bc321d0a7c4f5391aa83ecf315cb8a47ff4cf090/app/src/main/java/com/termux/filepicker/TermuxDocumentsProvider.java)
  - This file is now called DocumentProvider and has been modified for the purposes of the project. The changes are tracked using git.
  - This file is licensed under GPLv3 which can be found [here](LICENSE)
[Marco Mastropaolo](https://github.com/xanathar) - [Moonsharp](https://github.com/moonsharp-devs/moonsharp)
  - Moonsharp is used as the Lua interpreter for [Create Your Frisk](https://github.com/RhenaudTheLukark/CreateYourFrisk) and this port
  - Moonsharp is licensed under All Rights Reserved and its terms can be found [here](MOONSHARP_LICENSE)

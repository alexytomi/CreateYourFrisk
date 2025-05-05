# Create Your Frisk - Lua moddable Undertale engine

Welcome to the Git repository for **Create Your Frisk**, a fork of [**Unitale**](https://github.com/lvk/Unitale/) by lvk!

When testing, you'll want to load the Disclaimer scene from the Scenes folder.

Editing the Overworld requires **Unity** (see **Unity Version** below).

We also have a [**Discord server**](https://discord.gg/GFJ5277)! Feel free to check it out if you want to be in touch with Unitale and Create Your Frisk's community!

***

## Download

To download the engine, go to [**the releases page**](https://github.com/RhenaudTheLukark/CreateYourFrisk/releases).

***

## Unity Version

CYF's latest version is **v0.6.6**. It was built using **Unity Personal 2018.4.36f1**, or Unity 2018's **Long Term Support** version.

To get this version of Unity, either:

* [**Download the Unity Hub**](https://unity3d.com/get-unity/download) or
* [**Find it in the Unity Version Archive**](https://unity.com/releases/editor/whats-new/2018.4.36) (remember, it's **2018.4.36**).

***

## Building

You will need to use IL2CPP if you want this to run on most phones (arm64) because Unity

Requires 
  - Android SDK Tools 26.1.1
    Can be obtained using: 
      - `sdkmanager --install tools`
      - Android Studio SDK Manager (Disable "Hide Obsolete Packages")
  - Android NDK 16.1.4479499
    Can be obtained using: 
      - `sdkmanager --install ndk;16.1.4479499`
      - Android Studio SDK Manager (Enable "Show Package Details")
    **Note: You must specify the directory in Edit > Preferences > External Tools (it's on the Sidebar) > NDK**
    If you do not specify the directory, it may use the incorrect NDK version and spit out a mystical error

In addition, Unity might crash on launch or crash while building due to missing native libraries. The solution for these issues are dependent on whatever platform you're on. You are on your own. Sorry.

***

## Building

Requires 
  - Android SDK Tools 26.1.1
    Can be obtained using: 
      - `sdkmanager --install tools`
      - Android Studio SDK Manager (Disable "Hide Obsolete Packages")
  - Android NDK 16.1.4479499
    Can be obtained using: 
      - `sdkmanager --install ndk;16.1.4479499`
      - Android Studio SDK Manager (Enable "Show Package Details")
    **Note: You must specify the directory in Edit > Preferences > External Tools (it's on the Sidebar) > NDK**
    If you do not specify the directory, it may use the incorrect NDK version and spit out a mystical error
 
***

## Required files

Please keep the mods and the resources which are in the Mods and Default folders, unless you know what you're doing.
~~The only mods you can remove safely are Mionn and Donald Trump.~~ *(Removed as of CYF v0.6.2)*

***

## Licenses

Create Your Frisk is released under the GNU General Public License 3.0.
We are using MoonSharp as our Lua interpreter, written by Marco Mastropaolo. The binary is included in `/Assets/Plugins`. License details in `MOONSHARP_LICENSE`.

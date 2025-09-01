# Shin Ryu Mod Manager-CE
Mod manager for Yakuza series PC games. Please check the [Supported Games](../../wiki/Supported-Games) list before using.

Allows loading mods from a `/mods/` folder inside the game's directory.
Mods do not have to contain repacked PAR archives, as Shin Ryu Mod Manager can load loose files from the mod folder.
Repacking is needed only for some PAR archives in Old Engine games (Yakuza games released before Yakuza 6). Other games do not need any PAR repacking.

> [!NOTE]
>
> This is a port of the regular [ShinRyuModManager](https://github.com/SRMM-Studio/ShinRyuModManager) project to .NET 8 and to ben cross-platform.

# Installing
Unpack the [latest release](../../releases/latest) into the game's directory, in the same folder as the game's executable.

# Usage
A command line interface is available, as well as a GUI.

For actual usage, check the [Installing Mods](../../wiki/Installing-Mods) and [Creating A New Mod](../../wiki/Creating-A-New-Mod) articles in the [wiki](../../wiki).

To run the program, you can launch it with no arguments to open the GUI, or run the file in a terminal with the argument `--cli` to open it in CLI mode. Either method will generate an MLO file to be used by [Parless](https://github.com/SRMM-Studio/YakuzaParless), the Yakuza mod loader.
All the mod manager [releases](../../releases) include Parless and all necessary files for usage, so no need to download Parless separately.

### CLI Mode

#### Windows
```powershell
> .\ShinRyuModManager.exe --cli
```

#### Linux
```sh
$ ./ShinRyuModManager-CE --cli
```

# Building
Clone the repository, then open the solution file (.sln) in Visual Studio. You can then `dotnet publish` the `ShinRyuModManager-CE` project.

# Differences/Known Issues
Given this is a port, and a cross-platform one at that, there are likely going to be differences between this version and the original.
Here will be listed the known differences and issues that the port brings.

- Opening the CLI version with `Left-CTRL` doesn't work as there's no good way to detect this on all supported platforms.
- The updater doesn't work, as it relies on infrastructure that only supports the original version. May be supported in the future.
- Console output doesn't currently work on Windows.
- MessageBoxes are different. This is strictly because Avalonia [doesn't have native support](https://docs.avaloniaui.net/docs/basics/user-interface/messagebox) yet, but it does look to be planned for the next major release.
- On Linux you must run in CLI mode, or press the "Save mod list" button for the `.mlo` to generate. This is because Parless only searches for the Windows `.exe`.

# Credits
Original project by [SutandoTsukai181](https://github.com/SutandoTsukai181).

Original ShinRyuModManager by [SRMM-Studio](https://github.com/SRMM-Studio).

Thanks to [Kaplas](https://github.com/Kaplas80) for [ParLibrary](https://github.com/Kaplas80/ParManager), which is used for repacking pars.

Thanks to [Pleonex](https://github.com/pleonex) for [Yarhl](https://github.com/SceneGate/Yarhl).

Thanks to Kent for providing the logo and UI graphics.

For the mod loader credits, please check the [YakuzaParless](https://github.com/SRMM-Studio/YakuzaParless) repository.

# License
This project uses the MIT License, so feel free to include it in whatever you want.

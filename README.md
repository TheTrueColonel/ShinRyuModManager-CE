# Shin Ryu Mod Manager-Linux
Mod manager for Yakuza series PC games. Please check the [Supported Games](../../wiki/Supported-Games) list before using.

Allows loading mods from a `/mods/` folder inside the game's directory.
Mods do not have to contain repacked PAR archives, as Shin Ryu Mod Manager can load loose files from the mod folder.
Repacking is needed only for some PAR archives in Old Engine games (Yakuza games released before Yakuza 6). Other games do not need any PAR repacking.

> [!NOTE]
>
> This is a port of the CLI code from the regular [ShinRyuModManager](https://github.com/SRMM-Studio/ShinRyuModManager) project to .NET 8 and to natively support Linux.
> There are differences in how the OSes handle files that might cause some issues when running this tool natively on Linux vs running SRMM via Wine.

# Installing
Unpack the [latest release](../../releases/latest) into the game's directory, in the same folder as the game's executable.

# Usage
```
$ ./ShinRyuModManager-Linux
```

# Building
Clone the repository and fetch the submodules, then open the solution file (.sln) in Visual Studio. You can then `dotnet publish` the `ShinRyuModManager-Linux` project.

# Credits
Original project by [SutandoTsukai181](https://github.com/SutandoTsukai181).

Original ShinRyuModManager by [SRMM-Studio](https://github.com/SRMM-Studio).

Thanks to [Kaplas](https://github.com/Kaplas80) for [ParLibrary](https://github.com/Kaplas80/ParManager), which is used for repacking pars.

Thanks to [Pleonex](https://github.com/pleonex) for [Yarhl](https://github.com/SceneGate/Yarhl).

<!--Thanks to Kent for providing the logo and UI graphics.-->

For the mod loader credits, please check the [YakuzaParless](https://github.com/SRMM-Studio/YakuzaParless) repository.

# License
This project uses the MIT License, so feel free to include it in whatever you want.

﻿using Microsoft.Win32;

namespace Utils;

public static class GamePath {
    public const string DATA = "data";
    public const string MODS = "mods";
    public const string LIBRARIES = "srmm-libs";
    
    public static Game CurrentGame { get; } = Game.Unsupported;
    public static string FullGamePath { get; }
    public static string DataPath { get; }
    public static string ModsPath { get; }
    public static string ExternalModsPath { get; }
    public static string LibrariesPath { get; }
    public static string GameExe { get; }
    
    static GamePath() {
        FullGamePath = Directory.GetCurrentDirectory();
        DataPath = Path.Combine(FullGamePath, DATA);
        ModsPath = Path.Combine(FullGamePath, MODS);
        ExternalModsPath = Path.Combine(ModsPath, Constants.EXTERNAL_MODS);
        LibrariesPath = Path.Combine(FullGamePath, LIBRARIES);
        
        // Try to get game
        foreach (var file in Directory.GetFiles(FullGamePath, "*.exe")) {
            if (!Enum.TryParse<Game>(Path.GetFileNameWithoutExtension(file), true, out var game))
                continue;
            
            CurrentGame = game;
            GameExe = $"{game}.exe";
            
            break;
        }
    }
    
    public static string GetBasename(string path) {
        return Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar));
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <returns>Given path but starting after /mods/ModName/ </returns>
    public static string RemoveModPath(string path) {
        var modsPos = path.IndexOf("mods" + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        
        return path[path.IndexOf(Path.DirectorySeparatorChar, modsPos + 5)..];
    }
    
    public static string RemoveParlessPath(string path) {
        var dataPos = path.IndexOf("data" + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        
        path = path.Replace(".parless", "");
        
        return path[(dataPos + 4)..];
    }
    
    public static string GetDataPathFrom(string path) {
        return path.Contains(".parless")
            ? path[(path.IndexOf("data" + Path.DirectorySeparatorChar, StringComparison.Ordinal) + 4)..] // Preserve .parless in path instead of removing it
            : RemoveModPath(path);
    }
    
    public static string GetModPathFromDataPath(string mod, string path) {
        return Path.Combine(ModsPath, mod, path.TrimStart(Path.DirectorySeparatorChar));
    }
    
    public static bool FileExistsInData(string path) {
        var dataPath = Path.Combine(DataPath, path.TrimStart(Path.DirectorySeparatorChar));
        
        return File.Exists(dataPath);
    }
    
    public static bool DirectoryExistsInData(string path) {
        var dataPath = Path.Combine(DataPath, path.TrimStart(Path.DirectorySeparatorChar));
        
        return Directory.Exists(dataPath);
    }
    
    public static string GetRootParPath(string path) {
        while (true) {
            if (!path.Contains(Path.DirectorySeparatorChar)) {
                return FileExistsInData(path) ? path : "";
            }
            
            if (FileExistsInData(path)) {
                return path;
            }
            
            path = path[..path.LastIndexOf(Path.DirectorySeparatorChar)] + ".par";
        }
    }
    
    public static bool ExistsInDataAsParNested(string path) {
        if (path.Contains(".parless")) {
            // Remove ".parless"
            return GetRootParPath(RemoveParlessPath(path) + ".par") != "";
        }
        
        // Add ".par"
        return GetRootParPath(RemoveModPath(path) + ".par") != "";
    }
    
    public static bool ExistsInDataAsPar(string path) {
        return path.Contains(".parless") ?
            // Remove ".parless"
            FileExistsInData(RemoveParlessPath(path) + ".par") :
            // Add ".par"
            FileExistsInData(RemoveModPath(path) + ".par");
    }
    
    public static bool IsXbox(string path) {
        return path.Contains("Xbox") || path.Contains("WindowsApps") ||
            path.Contains(Path.DirectorySeparatorChar + "Content" + Path.DirectorySeparatorChar) ||
            File.Exists(Path.Combine(path, "MicrosoftGame.config"));
    }

    public static bool IsSteamInstalled() {
        if (OperatingSystem.IsWindows()) {
            return IsSteamInstalledWindows();
        }

        if (OperatingSystem.IsLinux()) {
            return IsSteamInstalledLinux();
        }

        return false;
    }

    private static bool IsSteamInstalledWindows() {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");

        return string.IsNullOrEmpty(key?.GetValue("SteamPath") as string);
    }
    
    private static bool IsSteamInstalledLinux() {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var potentialLocations = new List<string> {
            Path.Combine(home, ".steam/steam"),
            Path.Combine(home, ".local/share/Steam"),
            "/user/lib/steam"
        };

        return potentialLocations.Any(Directory.Exists);
    }
    
    public static string GetGameFriendlyName(Game g) {
        return g switch {
            Game.Yakuza0 => "Yakuza 0",
            Game.YakuzaKiwami => "Yakuza Kiwami",
            Game.YakuzaKiwami2 => "Yakuza Kiwami 2",
            Game.Yakuza3 => "Yakuza 3 Remastered",
            Game.Yakuza4 => "Yakuza 4 Remastered",
            Game.Yakuza5 => "Yakuza 5 Remastered",
            Game.Yakuza6 => "Yakuza 6",
            Game.YakuzaLikeADragon => "Yakuza: Like a Dragon",
            Game.Judgment => "Judgment",
            Game.LostJudgment => "Lost Judgment",
            Game.LikeADragonGaiden => "Like a Dragon Gaiden: The Man Who Erased His Name",
            Game.LikeADragon8 => "Like a Dragon: Infinite Wealth",
            Game.LikeADragonPirates => "Like a Dragon: Pirate Yakuza In Hawaii",
            Game.VFREVOBETA => "Virtua Fighter 5 R.E.V.O. Beta",
            Game.VFREVO => "Virtua Fighter 5 R.E.V.O.",
            Game.StrangerThanHeaven => "Stranger Than Heaven",
            Game.YakuzaKiwami3 => "Yakuza Kiwami 3",
            _ => "<unknown>"
        };
    }

    public static int? GetGameSteamId(Game game) {
        return game switch {
            Game.Yakuza0 => 638970,
            Game.YakuzaKiwami => 834530,
            Game.YakuzaKiwami2 => 927380,
            Game.Yakuza3 => 1088710,
            Game.Yakuza4 => 1105500,
            Game.Yakuza5 => 1105510,
            Game.Yakuza6 => 1388590,
            Game.YakuzaLikeADragon => 1235140,
            Game.Judgment => 2058180,
            Game.LostJudgment => 2058190,
            Game.LikeADragonGaiden => 2375550,
            Game.LikeADragon8 => 2072450,
            Game.LikeADragonPirates => 3061810,
            Game.VFREVOBETA => 3283250,
            Game.VFREVO => 3112260,
            Game.StrangerThanHeaven => null,
            Game.YakuzaKiwami3 => null,
            _ => null
        };
    }
}

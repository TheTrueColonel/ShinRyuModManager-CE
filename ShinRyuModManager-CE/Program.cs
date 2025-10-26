﻿using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Avalonia;
using Avalonia.Svg.Skia;
using IniParser;
using IniParser.Model;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Json;
using ShinRyuModManager.Helpers;
using ShinRyuModManager.ModLoadOrder;
using ShinRyuModManager.ModLoadOrder.Mods;
using ShinRyuModManager.Templates;
using ShinRyuModManager.UserInterface;
using Utils;
using Constants = Utils.Constants;

namespace ShinRyuModManager;

public static class Program {
    private static bool _externalModsOnly = true;
    private static bool _looseFilesEnabled;
    private static bool _cpkRepackingEnabled = true;
    private static bool _checkForUpdates = true;
    private static bool _isSilent;
    private static bool _migrated;
    private static IniData _iniData;
    
    private static readonly FileIniDataParser IniParser;

    public static bool RebuildMlo { get; private set; } = true;
    public static bool IsRebuildMloSupported { get; private set; } = true;
    public static LogEventLevel LogLevel { get; private set; } = LogEventLevel.Information;
    public static List<LibMeta> LibraryMetaCache { get; set; } = [];

    static Program() {
        IniParser = new FileIniDataParser {
            Parser = {
                Configuration = {
                    AssigmentSpacer = string.Empty
                }
            }
        };
    }
    
    [STAThread]
    private static void Main(string[] args) {
        Directory.CreateDirectory(Settings.LOGS_BASE_PATH);
        
        var defaultLogsPath = Path.Combine(Settings.LOGS_BASE_PATH, "srmm_logs.log");
        var errorLogsPath = Path.Combine(Settings.LOGS_BASE_PATH, "srmm_errors.log");
        
        LoadConfig();
        
        // Create global logger
        Log.Logger = new LoggerConfiguration()
                     .MinimumLevel.ControlledBy(new LoggingLevelSwitch(LogLevel))
                     .Enrich.WithExceptionDetails()
                     // Log to SRMM logs file
                     .WriteTo.Logger(l => l
                                          .Filter.ByIncludingOnly(e => e.Exception == null)
                                          .WriteTo.Async(a => a.File(defaultLogsPath, rollingInterval: RollingInterval.Day))
                                          .WriteTo.Async(a => a.Console()))
                     // Logs exceptions separately
                     .WriteTo.Logger(l => l
                                          .Filter.ByIncludingOnly(e => e.Exception != null)
                                          .WriteTo.Async(a => a.File(new JsonFormatter(renderMessage: true), errorLogsPath, rollingInterval: RollingInterval.Day)))
                     .CreateLogger();

        // TODO: Temporary, YakuzaParless.asi currently only supports the Windows binary. Currently disabling RebuildMLO on Linux
        if (!OperatingSystem.IsWindows()) {
            IsRebuildMloSupported = false;
        }
        
        // Check if there are any args, if so, run in CLI mode
        // Unfortunately, no one way to detect left Ctrl while being cross-platform
        if (args.Length == 0) {
            if (_checkForUpdates) {
                // TODO: Implement updates
            }
            
            Log.Information("Shin Ryu Mod Manager GUI Application Start");
            
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        } else {
            Log.Information("Shin Ryu Mod Manager CLI Mode Start");
            
            MainCLI(args).GetAwaiter().GetResult();
        }
    }
    
    private static AppBuilder BuildAvaloniaApp() {
        GC.KeepAlive(typeof(SvgImageExtension).Assembly);
        GC.KeepAlive(typeof(Svg.Skia.SKSvg).Assembly);
        
        return AppBuilder.Configure<App>()
                         .UsePlatformDetect()
                         .WithInterFont()
                         .LogToTrace();
    }
    
    private static async Task MainCLI(string[] args) {
        Log.Information("Shin Ryu Mod Manager-CE v{Version}", AssemblyVersion.GetVersion());
        Log.Information("By TheTrueColonel (a port of SRMM Studio's work)");
        
        // Parse arguments
        var list = new List<string>(args);
        
        if (list.Contains("-h") || list.Contains("--help")) {
            Log.Information("Usage: run without arguments to generate mod load order.");
            
            Log.Information("       run with \"-s\" or \"--silent\" flag to prevent checking for updates and remove prompts.");
            
            //Log.Information("       run with \"-r\" or \"--run\" flag to run the game after the program finishes.");
            Log.Information("       run with \"-h\" or \"--help\" flag to show this message and exit.");
            
            return;
        }
        
        if (list.Contains("-s") || list.Contains("--silent")) {
            _isSilent = true;
        }
        
        await RunGeneration(ConvertNewToOldModList(PreRun()));
        PostRun();

        await Log.CloseAndFlushAsync();

        // TODO: Update with logic from the UI
        /*if (list.Contains("-r") || list.Contains("--run")) {
            if (File.Exists(GamePath.GameExe)) {
                Console.WriteLine($"Launching \"{GamePath.GameExe}\"...");
                Process.Start(GamePath.GameExe);
            } else {
                Console.WriteLine($"Warning: Could not run game because \"{GamePath.GameExe}\" does not exist.");
            }
        }*/
    }

    private static void LoadConfig() {
        if (File.Exists(Constants.INI)) {
            _iniData = IniParser.ReadFile(Constants.INI);
            
            if (_iniData.TryGetKey("Overrides.LooseFilesEnabled", out var looseFiles)) {
                _looseFilesEnabled = int.Parse(looseFiles) == 1;
            }
            
            if (_iniData.TryGetKey("RyuModManager.Verbose", out var verbose)) {
                if (int.Parse(verbose) == 1) {
                    LogLevel = LogEventLevel.Verbose;
                }
            }
            
            if (_iniData.TryGetKey("RyuModManager.CheckForUpdates", out var check)) {
                _checkForUpdates = int.Parse(check) == 1;
            }
            
            if (_iniData.TryGetKey("RyuModManager.ShowWarnings", out var showWarnings)) {
                //ConsoleOutput.ShowWarnings = int.Parse(showWarnings) == 1;
            }
            
            if (_iniData.TryGetKey("RyuModManager.LoadExternalModsOnly", out var extMods)) {
                _externalModsOnly = int.Parse(extMods) == 1;
            }
            
            if (_iniData.TryGetKey("Overrides.RebuildMLO", out var rebuildMlo)) {
                RebuildMlo = int.Parse(rebuildMlo) == 1;
            }
            
            if (!_iniData.TryGetKey("Parless.IniVersion", out var iniVersion) ||
                int.Parse(iniVersion) < ParlessIni.CURRENT_VERSION) {
                // Update if ini version is old (or does not exist)
                Log.Information($"{Constants.INI} is outdated. Updating ini to the latest version... ");
                
                if (int.Parse(iniVersion) <= 3) {
                    // Force enable RebuildMLO option
                    _iniData.Sections["Overrides"]["RebuildMLO"] = "1";
                    RebuildMlo = true;
                }
                
                IniParser.WriteFile(Constants.INI, IniTemplate.UpdateIni(_iniData));
                Log.Information($"Updated {Constants.INI}");
            }
        } else {
            // Create ini if it does not exist
            Log.Information($"{Constants.INI} was not found. Creating default ini...");
            IniParser.WriteFile(Constants.INI, IniTemplate.NewIni());
        }
    }

    internal static List<ModInfo> PreRun() {
        if (GamePath.CurrentGame != Game.Unsupported && !Directory.Exists(GamePath.MODS)) {
            if (!Directory.Exists(GamePath.MODS)) {
                // Create mods folder if it does not exist
                Log.Information($"\"{GamePath.MODS}\" folder was not found. Creating empty folder... ");
                Directory.CreateDirectory(GamePath.MODS);
            }
            
            if (!Directory.Exists(GamePath.LIBRARIES)) {
                // Create libraries folder if it does not exist
                Log.Information($"\"{GamePath.LIBRARIES}\" folder was not found. Creating empty folder... ");
                Directory.CreateDirectory(GamePath.LIBRARIES);
            }
        }
        
        // TODO: Maybe move this to a separate "Game patches" file
        // Virtua Fighter eSports crashes when used with dinput8.dll as the ASI loader
        if (GamePath.CurrentGame == Game.Eve && File.Exists(Constants.DINPUT8DLL)) {
            if (File.Exists(Constants.VERSIONDLL)) {
                Log.Warning($"Game specific patch: Deleting {Constants.DINPUT8DLL} because {Constants.VERSIONDLL} exists...");
                
                // Remove dinput8.dll
                File.Delete(Constants.DINPUT8DLL);
            } else {
                Log.Warning($"Game specific patch: Renaming {Constants.DINPUT8DLL} to {Constants.VERSIONDLL}...");
                
                // Rename dinput8.dll to version.dll to prevent the game from crashing
                File.Move(Constants.DINPUT8DLL, Constants.VERSIONDLL);
            }
        } else if (GamePath.CurrentGame is Game.Judgment or Game.LostJudgment) {
            // Lost Judgment (and Judgment post update 1) does not like Ultimate ASI Loader, so instead we use a custom build of DllSpoofer (https://github.com/Kazurin-775/DllSpoofer)
            if (File.Exists(Constants.DINPUT8DLL)) {
                Log.Warning($"Game specific patch: Deleting {Constants.DINPUT8DLL} because it causes crashes with Judgment games...");
                
                // Remove dinput8.dll
                File.Delete(Constants.DINPUT8DLL);
            }
            
            if (!File.Exists(Constants.WINMMDLL)) {
                if (File.Exists(Constants.WINMMLJ)) {
                    Log.Warning($"Game specific patch: Enabling {Constants.WINMMDLL} by renaming {Constants.WINMMLJ} to fix Judgment games crashes...");
                    
                    // Rename dinput8.dll to version.dll to prevent the game from crashing
                    File.Move(Constants.WINMMLJ, Constants.WINMMDLL);
                } else {
                    Log.Error($"WARNING: {Constants.WINMMLJ} was not found. Judgment games will NOT load mods without this file. Please redownload Shin Ryu Mod Manager.");
                }
            }
        }
        
        // Read ini (again) to check if we should try importing the old load order file
        _iniData = IniParser.ReadFile(Constants.INI);
            
        if (GamePath.CurrentGame is Game.Judgment or Game.LostJudgment or Game.LikeADragonPirates) {
            // Disable RebuildMLO when using an external mod manager
            if (_iniData.TryGetKey("Overrides.RebuildMLO", out _)) {
                Log.Warning("Game specific patch: Disabling RebuildMLO for some games when using an external mod manager...");
                    
                _iniData.Sections["Overrides"]["RebuildMLO"] = "0";
                IniParser.WriteFile(Constants.INI, _iniData);
            }
        }
        
        var mods = new List<ModInfo>();
        
        if (ShouldBeExternalOnly()) {
            // Only load the files inside the external mods path, and ignore the load order in the txt
            mods.Add(new ModInfo(Constants.EXTERNAL_MODS));
        } else {
            var defaultEnabled = true;
            
            if (File.Exists(Constants.TXT_OLD) && _iniData.GetKey("SavedSettings.ModListImported") == null) {
                // Scanned mods should be disabled, because that's how they were with the old txt format
                defaultEnabled = false;
                
                // Set a flag so we can delete the old file after we actually save the mod list
                _migrated = true;
                
                // Migrate old format to new
                Log.Information("Old format load order file (" + Constants.TXT_OLD + ") was found. Importing to the new format...");
                
                mods.AddRange(ConvertOldToNewModList(ReadModLoadOrderTxt(Constants.TXT_OLD))
                    .Where(n => !mods.Any(m => EqualModNames(m.Name, n.Name))));
            } else if (File.Exists(Constants.TXT)) {
                mods.AddRange(ReadModListTxt(Constants.TXT).Where(n => !mods.Any(m => EqualModNames(m.Name, n.Name))));
            } else {
                Log.Information($"{Constants.TXT} was not found. Will load all existing mods.\n");
            }
            
            if (Directory.Exists(GamePath.MODS)) {
                // Add all scanned mods that have not been added to the load order yet
                Log.Information("Scanning for mods...");
                
                mods.AddRange(ScanMods().Where(n => !mods.Any(m => EqualModNames(m.Name, n)))
                                        .Select(m => new ModInfo(m, defaultEnabled)));
                
                Log.Information("Found {ModsCount} mods.", mods.Count);
            }
        }
        
        if (!GamePath.IsXbox(Path.Combine(GamePath.FullGamePath)))
            return mods;
        
        if (!_iniData.TryGetKey("Overrides.RebuildMLO", out _))
            return mods;
        
        Log.Warning("Game specific patch: Disabling RebuildMLO for Xbox games...");
        
        _iniData.Sections["Overrides"]["RebuildMLO"] = "0";
        IniParser.WriteFile(Constants.INI, _iniData);
        
        return mods;
    }
    
    internal static async Task RunGeneration(List<string> mods) {
        if (File.Exists(Constants.MLO)) {
            Log.Information("Removing old MLO...");
            
            // Remove existing MLO file to avoid it being used if a new MLO won't be generated
            File.Delete(Constants.MLO);
        }
        
        // Remove previously repacked pars, to avoid unwanted side effects
        ParRepacker.RemoveOldRepackedPars();
        
        if (GamePath.CurrentGame != Game.Unsupported) {
            if (mods is { Count: > 0 } || _looseFilesEnabled) {
                // Create Parless mod as highest priority
                mods.Remove("Parless");
                mods.Insert(0, "Parless");

                Directory.CreateDirectory(Constants.PARLESS_MODS_PATH);

                Log.Information("Generating MLO...");

                var sw = Stopwatch.StartNew();
                
                var result = await Generator.GenerateModeLoadOrder(mods, _looseFilesEnabled, _cpkRepackingEnabled);
                
                if (GameModel.SupportsUBIK(GamePath.CurrentGame)) {
                    GameModel.DoUBIKProcedure(result);
                }

                switch (GamePath.CurrentGame) {
                    case Game.Yakuza5:
                        GameModel.DoY5HActProcedure(result);
                        break;
                    
                    case Game.Yakuza0:
                    case Game.YakuzaKiwami:
                        GameModel.DoOEHActProcedure(result);
                        break;
                    
                    case Game.YakuzaKiwami2:
                        GameModel.DoDEHActProcedure(result, "lexus2");
                        break;
                    
                    case Game.Judgment:
                        GameModel.DoDEHActProcedure(result, "judge");
                        break;
                    
                    case Game.YakuzaLikeADragon:
                        GameModel.DoDEHActProcedure(result, "yazawa");
                        break;
                    
                    case Game.LikeADragonGaiden:
                        GameModel.DoDEHActProcedure(result, "aston");
                        break;
                    
                    case Game.LostJudgment:
                        GameModel.DoDEHActProcedure(result, "coyote");
                        break;
                    
                    case Game.LikeADragon8:
                        GameModel.DoDEHActProcedure(result, "elvis");
                        break;
                    
                    case Game.LikeADragonPirates:
                        GameModel.DoDEHActProcedure(result, "spr");
                        break;
                    
                    case Game.YakuzaKiwami3:
                        GameModel.DoDEHActProcedure(result, "lexus3");
                        break;
                }
                
                sw.Stop();
                Log.Information("MLO Generation took: {ElapsedTotalSeconds} seconds", sw.Elapsed.TotalSeconds);

                return;
            }
            
            Log.Warning("Aborting: No mods were found, and .parless paths are disabled");
        }
        
        Log.Warning("Aborting: No supported game was found in this directory");
    }

    private static void PostRun() {
        // Check if the ASI loader is not in the directory (possibly due to incorrect zip extraction)
        if (MissingDll()) {
            Log.Warning($"Warning: \"{Constants.DINPUT8DLL}\" is missing from this directory. Shin Ryu Mod Manager will NOT function properly without this file");
        }

        // Check if the ASI is not in the directory
        if (MissingAsi()) {
            Log.Warning($"Warning: \"{Constants.ASI}\" is missing from this directory. Shin Ryu Mod Manager will NOT function properly without this file");
        }

        // Calculate the checksum for the game's exe to inform the user if their version might be unsupported
        if (LogLevel <= LogEventLevel.Warning && InvalidGameExe()) {
            Log.Error("Warning: Game version is unrecognized. Please use the latest Steam version of the game.");
            Log.Error("Shin Ryu Mod Manager will still generate the load order, but the game might CRASH or not function properly");
        }

        if (!_isSilent) {
            Log.Information("Program finished. Press any key to exit...");
            Console.ReadKey();
        }
    }
    
    private static List<string> ReadModLoadOrderTxt(string txt) {
        if (!File.Exists(txt)) {
            return [];
        }
        
        var mods = new HashSet<string>();
        
        foreach (var line in File.ReadLines(txt)) {
            if (line.StartsWith(';'))
                continue;
            
            var sanitizedLine = line.Split(';', 1)[0].Trim();
            
            // Add only valid and unique mods
            if (!string.IsNullOrEmpty(sanitizedLine) &&
                Directory.Exists(Path.Combine(GamePath.MODS, sanitizedLine))) {
                mods.Add(sanitizedLine);
            }
        }
        
        return mods.ToList();
    }
    
    private static List<ModInfo> ConvertOldToNewModList(List<string> mods) {
        return mods.Select(m => new ModInfo(m)).ToList();
    }
    
    internal static List<string> ConvertNewToOldModList(List<ModInfo> mods) {
        return mods.Where(m => m.Enabled).Select(m => m.Name).ToList();
    }
    
    internal static bool ShouldBeExternalOnly() {
        return _externalModsOnly && Directory.Exists(GamePath.ExternalModsPath);
    }
    
    private static List<string> ScanMods() {
        return Directory.GetDirectories(GamePath.ModsPath)
                        .Select(d => Path.GetFileName(d.TrimEnd(Path.DirectorySeparatorChar)))
                        .Where(m => !string.Equals(m, "Parless") && !string.Equals(m, Constants.EXTERNAL_MODS))
                        .ToList();
    }
    
    private static bool EqualModNames(string m, string n) {
        return string.Compare(m, n, StringComparison.InvariantCultureIgnoreCase) == 0;
    }

    internal static bool MissingDll() {
        return !(File.Exists(Constants.DINPUT8DLL) || File.Exists(Constants.VERSIONDLL) || File.Exists(Constants.WINMMDLL));
    }

    internal static bool MissingAsi() {
        return !File.Exists(Constants.ASI);
    }

    internal static bool InvalidGameExe() {
        return false;

        /*
        string path = Path.Combine(GetGamePath(), GetGameExe());
        return GetGame() == Game.Unsupported || !GameHash.ValidateFile(path, GetGame());
        */
    }

    public static List<ModInfo> ReadModListTxt(string text) {
        var mods = new List<ModInfo>();

        if (!File.Exists(text)) {
            return mods;
        }

        using var file = new StreamReader(new FileInfo(text).FullName);
        var line = file.ReadLine();

        if (line == null)
            return mods;

        foreach (var mod in line.Split('|', StringSplitOptions.RemoveEmptyEntries)) {
            if (!mod.StartsWith('<') && !mod.StartsWith('>'))
                continue;

            var info = new ModInfo(mod[1..], mod[0] == '<');

            if (ModInfo.IsValid(info) && !mods.Contains(info)) {
                mods.Add(info);
            }
        }

        return mods;
    }

    internal static async Task<bool> SaveModListAsync(List<ModInfo> mods) {
        var result = await WriteModListTextAsync(mods);

        if (!_migrated)
            return result;

        try {
            File.Delete(Constants.TXT_OLD);

            var iniParser = new FileIniDataParser();
            iniParser.Parser.Configuration.AssigmentSpacer = string.Empty;

            var ini = iniParser.ReadFile(Constants.INI);

            ini.Sections.AddSection("SavedSettings");
            ini["SavedSettings"].AddKey("ModListImported", "true");
            iniParser.WriteFile(Constants.INI, ini);
        } catch {
            Log.Warning($"Could not delete {Constants.TXT_OLD}. This file should be deleted manually.");
        }

        return result;
    }

    private static async Task<bool> WriteModListTextAsync(List<ModInfo> mods) {
        if (mods == null || mods.Count == 0)
            return false;

        var sb = new StringBuilder();

        foreach (var mod in mods) {
            sb.Append($"{(mod.Enabled ? "<" : ">")}{mod.Name}|");
        }

        // Remove leftover pipe
        sb.Length -= 1;
        
        await File.WriteAllTextAsync(Constants.TXT, sb.ToString());

        return true;
    }
    
    public static string GetModDirectory(string mod)
    {
        return Path.Combine(GamePath.ModsPath, mod);
    }
    
    public static string[] GetModDependencies(string mod)
    {
        string modDir = GetModDirectory(mod);

        if (!Directory.Exists(modDir))
            return [];

        string metaFile = Path.Combine(modDir, "mod-meta.yaml");

        if (!File.Exists(metaFile))
            return [];
        
        var meta = YamlHelpers.DeserializeYamlFromPath<ModMeta>(metaFile);

        if (string.IsNullOrEmpty(meta.Dependencies))
            return [];

        return meta.Dependencies.Split(';');
    }

    public static string GetLibraryPath(string guid)
    {
        return Path.Combine(GamePath.LibrariesPath, guid);
    }

    public static string GetLocalLibraryCopyPath()
    {
        return Path.Combine(GamePath.LibrariesPath, Settings.LIBRARIES_INFO_REPO_FILE_PATH);
    }

    //Read cached data at startup if it exists
    public static void ReadCachedLocalLibraryData()
    {
        string path = GetLocalLibraryCopyPath();

        if (!File.Exists(path))
            return;

        LibMeta.ReadLibMetaManifest(File.ReadAllText(path));
    }

    public static LibMeta GetLibMeta(string guid)
    {
        return LibraryMetaCache.FirstOrDefault(x => x.GUID.ToString() == guid);
    }

    public static bool DoesLibraryExist(string guid)
    {
        string libDir = GetLibraryPath(guid);

        if (!Directory.Exists(libDir))
            return false;

        return true;
    }

    public static bool IsLibraryEnabled(string guid)
    {
        if (!DoesLibraryExist(guid))
            return false;

        if (File.Exists(Path.Combine(GetLibraryPath(guid), ".disabled")))
            return false;

        return true;
    }

    public static string GetLibraryName(string guid)
    {
        var metaData = GetLibMeta(guid);

        if (metaData != null)
            return metaData.Name;

        var path = Path.Combine(GetLibraryPath(guid), Settings.LIBRARIES_LIBMETA_FILE_NAME);

        if (!File.Exists(path))
            return guid;

        var yamlString = File.ReadAllText(path);
        var meta = LibMeta.ReadLibMeta(yamlString);
            
        return meta.Name;
    }

    public static async Task InstallLibraryAsync(string guid) {
        var metaFile = GetLibMeta(guid);

        if (metaFile == null || string.IsNullOrEmpty(metaFile.Download))
            return;

        var packagePath = await DownloadLibraryPackageAsync($"{guid}.zip", metaFile);
        
        var destDir = Path.Combine(GamePath.LibrariesPath, metaFile.GUID.ToString());
        
        if (Directory.Exists(destDir))
            Directory.Delete(destDir, true);
            
        Directory.CreateDirectory(destDir);
        ZipFile.ExtractToDirectory(packagePath, destDir, true);
    }
    
    private static async Task<string> DownloadLibraryPackageAsync(string fileName, LibMeta meta) {
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Settings.TEMP_DIRECTORY_NAME));

        try {
            var path = Path.Combine(Path.GetTempPath(), Settings.TEMP_DIRECTORY_NAME, fileName);

            await using var stream = await Utils.Client.GetStreamAsync(meta.Download);
            await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);

            await stream.CopyToAsync(fs);
            await fs.FlushAsync();

            return path;
        } catch (Exception ex) {
            Log.Error(ex, "Failed to download library!");

            return string.Empty;
        }
    }

    public static async Task InstallAllModDependenciesAsync() {
        try {
            await LibMeta.FetchAsync();
        } catch {
            // ignored
        }

        var modList = ReadModListTxt(Constants.TXT);

        foreach (var mod in modList.Where(x => x.Enabled)) {
            await InstallModDependenciesAsync(mod.Name);
        }
    }

    public static async Task InstallModDependenciesAsync(string mod) {
        var modDir = GetModDirectory(mod);
        
        if (!Directory.Exists(modDir))
            return;

        var metaFile = Path.Combine(modDir, "mod-meta.yaml");
        
        if (!File.Exists(metaFile))
            return;

        var meta = await YamlHelpers.DeserializeYamlFromPathAsync<ModMeta>(metaFile);
        
        if (string.IsNullOrEmpty(meta.Dependencies))
            return;

        foreach (var dep in meta.Dependencies.Split(';')) {
            if (!DoesLibraryExist(dep)) {
                await InstallLibraryAsync(dep);
            }
        }
    }
}

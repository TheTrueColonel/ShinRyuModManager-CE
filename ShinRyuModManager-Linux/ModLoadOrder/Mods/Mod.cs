using System.Text;
using Utils;

namespace ShinRyuModManager.ModLoadOrder.Mods;

public class Mod {
    public string Name { get; }
    
    /// <summary>
    /// Files that can be directly loaded from the mod path.
    /// </summary>
    public List<string> Files { get; }
    
    /// <summary>
    /// Folders that have to be repacked into pars before running the game.
    /// </summary>
    public List<string> ParFolders { get; }
    
    /// <summary>
    /// Folders that need to be bound as a directory to a CPK binder.
    /// </summary>
    public List<string> CpkFolders { get; }
    
    /// <summary>
    /// Folders that need to be repacked.
    /// </summary>
    public List<string> RepackCpKs { get; }
    
    protected readonly ConsoleOutput ConsoleOutput;
    
    public Mod(string name, int indent = 2) {
        Name = name;
        Files = [];
        ParFolders = [];
        CpkFolders = [];
        RepackCpKs = [];
        
        ConsoleOutput = new ConsoleOutput(indent);
        ConsoleOutput.WriteLine($"Reading directory: {name} ...");
    }
    
    public void PrintInfo() {
        ConsoleOutput.WriteLineIfVerbose();
        
        if (Files.Count > 0 || ParFolders.Count > 0) {
            if (Files.Count > 0) {
                ConsoleOutput.WriteLine($"Added {Files.Count} file(s)");
            }
            
            if (ParFolders.Count > 0) {
                ConsoleOutput.WriteLine($"Added {ParFolders.Count} folder(s) to be repacked");
            }
            
            if (CpkFolders.Count > 0) {
                ConsoleOutput.WriteLine($"Added {CpkFolders.Count} CPK folder(s) to be bound");
            }
        } else {
            ConsoleOutput.WriteLine($"Nothing found for {Name}, skipping");
        }
        
        ConsoleOutput.Flush();
    }
    
    public void AddFiles(string path, string check) {
        var needsRepack = false;
        var basename = GamePath.GetBasename(path);
        
        // Check if this path does not need repacking
        if (Name != "Parless") {
            switch (check) {
                case "chara":
                case "map_":
                case "effect":
                    needsRepack = GamePath.ExistsInDataAsPar(path);
                    
                    break;
                case "prep":
                case "light_anim":
                    needsRepack = GamePath.CurrentGame < Game.Yakuza0 && GamePath.ExistsInDataAsPar(path);
                    
                    break;
                case "2d":
                case "cse":
                    needsRepack = (basename.StartsWith("sprite") || basename.StartsWith("pj")) &&
                        GamePath.ExistsInDataAsParNested(path);
                    
                    break;
                case "pausepar":
                    if (GamePath.CurrentGame >= Game.Yakuza0)
                        needsRepack = true;
                    else
                        needsRepack = !basename.StartsWith("pause") && GamePath.ExistsInDataAsPar(path);
                    
                    break;
                case "pausepar_e":
                    needsRepack = !basename.StartsWith("pause") && GamePath.ExistsInDataAsPar(path);
                    
                    break;
                case "particle":
                    if (GamePath.CurrentGame >= Game.Yakuza6 && basename == "arc") {
                        check = "particle/arc";
                    }
                    
                    if (new DirectoryInfo(path).Parent?.Name == "arc_list")
                        needsRepack = true;
                    
                    break;
                case "particle/arc":
                    needsRepack = GamePath.ExistsInDataAsParNested(path);
                    
                    break;
                case "stage":
                    needsRepack = GamePath.CurrentGame == Game.Eve && basename == "sct" &&
                        GamePath.ExistsInDataAsParNested(path);
                    
                    break;
                case "":
                    needsRepack = (basename == "ptc" && GamePath.ExistsInDataAsParNested(path))
                        || (basename == "entity_adam" && GamePath.ExistsInDataAsPar(path));
                    
                    if (!needsRepack) {
                        check = CheckFolder(basename);
                    }
                    
                    break;
            }
        
            // Check for CPK directories
            string cpkDataPath;
        
            switch (basename) {
                case "bgm":
                    cpkDataPath = GamePath.RemoveModPath(path);
                    RepackCpKs.Add(cpkDataPath);
                
                    break;
            
                case "se":
                case "speech":
                    cpkDataPath = GamePath.RemoveModPath(path);
                
                    if (GamePath.CurrentGame == Game.Yakuza5) {
                        CpkFolders.Add(cpkDataPath + ".cpk");
                        ConsoleOutput.WriteLineIfVerbose($"Adding CPK folder: {cpkDataPath}");
                    } else {
                        RepackCpKs.Add(cpkDataPath + ".cpk");
                    }
                
                    break;
                case "stream":
                case "stream_en":
                case "stmdlc":
                case "stmdlc_en":
                case "movie":
                case "moviesd":
                case "moviesd_dlc":
                    cpkDataPath = GamePath.RemoveModPath(path);
                
                    if (GamePath.CurrentGame is Game.Judgment or Game.LostJudgment) {
                        CpkFolders.Add(cpkDataPath + ".par");
                        ConsoleOutput.WriteLineIfVerbose($"Adding CPK folder: {cpkDataPath}");
                    }
                
                    break;
                case "gv_files":
                    cpkDataPath = GamePath.RemoveModPath(path);
                
                    CpkFolders.Add($"{cpkDataPath}.cpk");
                    ConsoleOutput.WriteLineIfVerbose($"Adding CPK folder: {cpkDataPath}");

                    break;
            }

            if (GamePath.CurrentGame >= Game.LikeADragonPirates) {
                // Additional game specific checks
                switch (basename) {
                    // Pirates in Hawaii stores gmts inside folders based on the lowercase filename checksum.
                    // For the modder's convenience, move any gmts in the folder root to the corresponding subdirectory.
                    case "motion":
                    {
                        var gmtFolderPath = Path.Combine(path, "gmt");

                        if (!Directory.Exists(gmtFolderPath))
                            break;

                        var baseParlessPath = Path.Combine(GamePath.ModsPath, "Parless", "motion", "gmt");

                        foreach (var p in Directory.GetFiles(gmtFolderPath).Where(f => !f.EndsWith(Constants.VORTEX_MANAGED_FILE)).Select(GamePath.GetDataPathFrom)) {
                            // Copy any gmts to the appropriate hash folder in Parless
                            if (!p.EndsWith(".gmt", StringComparison.InvariantCultureIgnoreCase))
                                continue;

                            var gmtPath = Path.Combine(gmtFolderPath, Path.GetFileName(p));
                            var checksum = ((Func<string, string>)(s => (Encoding.UTF8.GetBytes(s).Sum(b => b) % 256).ToString("x2").PadLeft(4, '0')))(Path.GetFileNameWithoutExtension(p).ToLowerInvariant());
                            var destinationDirectory = Path.Combine(baseParlessPath, checksum);

                            if (!Directory.Exists(destinationDirectory))
                                Directory.CreateDirectory(destinationDirectory);
                        
                            File.Copy(gmtPath, Path.Combine(destinationDirectory, Path.GetFileName(gmtPath)));
                        }

                        break;
                    }
                }
            }
        }
        
        if (needsRepack) {
            var dataPath = GamePath.GetDataPathFrom(path);
            
            // Add this folder to the list of folders to be repacked and stop recursing
            ParFolders.Add(dataPath);
            ConsoleOutput.WriteLineIfVerbose($"Adding repackable folder: {dataPath}");
        } else {
            // Add files in current directory
            var files = Directory.GetFiles(path).Where(f => !f.EndsWith(Constants.VORTEX_MANAGED_FILE)).Select(GamePath.GetDataPathFrom);
            
            foreach (var p in files) {
                Files.Add(p);
                ConsoleOutput.WriteLineIfVerbose($"Adding file: {p}");
            }

            var isParlessMod = GetType() == typeof(ParlessMod);
            
            // Get files for all subdirectories
            foreach (var folder in Directory.GetDirectories(path)) {
                // Break an important rule in the concept of inheritance to make the program function correctly
                if (isParlessMod) {
                    ((ParlessMod)this).AddFiles(folder, check);
                } else {
                    AddFiles(folder, check);
                }
            }
        }
    }
    
    protected static string CheckFolder(string name) {
        foreach (var folder in Constants.IncompatiblePars.Where(name.StartsWith)) {
            return folder;
        }
        
        return "";
    }
}

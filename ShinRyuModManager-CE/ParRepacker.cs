using ParLibrary.Converter;
using Utils;
using Yarhl.FileSystem;

namespace ShinRyuModManager;

public static class ParRepacker {
    private static void DeleteDirectory(string targetDirectory) {
        File.SetAttributes(targetDirectory, FileAttributes.Normal);
        
        var files = Directory.GetFiles(targetDirectory);
        var dirs = Directory.GetDirectories(targetDirectory);
        
        foreach (var file in files) {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }
        
        foreach (var dir in dirs) {
            DeleteDirectory(dir);
        }
        
        Directory.Delete(targetDirectory, false);
    }
    
    public static void RemoveOldRepackedPars() {
        var pathToParlessMods = Path.Combine(GamePath.ModsPath, "Parless");
        
        if (!Directory.Exists(pathToParlessMods))
            return;
        
        Console.Write("Removing old pars...");
        
        try {
            DeleteDirectory(pathToParlessMods);
        } catch {
            Console.WriteLine($" FAIL! {pathToParlessMods}\n");
        }
        
        Console.WriteLine(" DONE!\n");
    }
    
    public static async Task RepackDictionary(Dictionary<string, List<string>> parDictionary) {
        var parTasks = new List<Task<ConsoleOutput>>();
        
        if (parDictionary.Count == 0) {
            Program.Log("No pars to repack.\n");
            
            return;
        }
        
        Program.Log("Repacking pars...\n");
        
        /*foreach (var parModPair in parDictionary) {
            var consoleOutput = new ConsoleOutput(2);
            
            parTasks.Add(Task.Run(() => RepackPar(parModPair.Key, parModPair.Value, consoleOutput)));
        }
        
        while (parTasks.Count > 0) {
            var console = await Task.WhenAny(parTasks);
            
            console.Result?.Flush();
            
            parTasks.Remove(console);
        }*/

        foreach (var parModPair in parDictionary) {
            var consoleOutput = new ConsoleOutput(2);

            RepackPar(parModPair.Key, parModPair.Value, consoleOutput);
            
            consoleOutput.Flush();
        }
        
        Program.Log($"Repacked {parDictionary.Count} par(s)!\n");
    }
    
    // TODO: Implement proper checking for a file being in use before repacking. Breaks parallelism
    private static ConsoleOutput RepackPar(string parPath, List<string> mods, ConsoleOutput console) {
        parPath = parPath.TrimStart(Path.DirectorySeparatorChar);
        
        var parPathReal = GamePath.GetRootParPath(parPath + ".par");
        var pathToPar = Path.Combine(GamePath.DataPath, parPathReal);
        var pathToModPar = Path.Combine(GamePath.ModsPath, "Parless", parPath + ".par");
        
        // Check if actual repackable par is nested
        if (parPath + ".par" != parPathReal) {
            // -4 to skip ".par", +1 to skip the directory separator
            parPathReal = parPath[(parPathReal.Length - 4 + 1)..] + ".par";
        } else {
            // Add the directory separators to properly search for the nodes
            parPathReal = Path.DirectorySeparatorChar + parPathReal;
        }
        
        // Normalize directory separators
        parPathReal = Utils.NormalizeToNodePath(parPathReal);
        
        // Dictionary of fileInPar, ModName
        var fileDict = new Dictionary<string, string>();
        
        Program.Log($"Repacking {parPath}.par...");
        
        // Populate fileDict with the files inside each mod
        foreach (var mod in mods) {
            foreach (var modFile in GetModFiles(parPath, mod, console)) {
                fileDict.TryAdd(modFile, mod);
            }
        }
        
        var pathToTempPar = pathToModPar + "temp";
        
        // Make sure that the .partemp directory is empty
        if (File.Exists(pathToModPar)) {
            File.Delete(pathToModPar);
        }
        
        // Make sure that the .partemp directory is empty
        if (Directory.Exists(pathToTempPar)) {
            DeleteDirectory(pathToTempPar);
        }
        
        Directory.CreateDirectory(pathToTempPar);
        
        // Copy each file in the mods to the .partemp directory
        foreach (var fileModPair in fileDict) {
            string fileInModFolder;
            
            if (fileModPair.Value.StartsWith(Constants.PARLESS_NAME)) {
                // 15 = ParlessMod.NAME.Length + 1
                fileInModFolder = Path.Combine(GamePath.DataPath, parPath.Insert(int.Parse(fileModPair.Value[15..]) - 1, ".parless"), fileModPair.Key);
            } else {
                fileInModFolder = GamePath.GetModPathFromDataPath(fileModPair.Value, Path.Combine(parPath, fileModPair.Key));
            }
            
            var fileInTempFolder = Path.Combine(pathToTempPar, fileModPair.Key);
            
            Directory.GetParent(fileInTempFolder)?.Create();
            File.Copy(fileInModFolder, fileInTempFolder, true);
        }
        
        var readerParams = new ParArchiveReaderParameters {
            Recursive = true
        };
        
        var writerParams = new ParArchiveWriterParameters {
            CompressorVersion = 0,
            OutputPath = pathToModPar,
            IncludeDots = true
        };
        
        // Create a node from the .partemp directory and write the par to pathToModPar
        var nodeName = new DirectoryInfo(pathToTempPar).Name;
        var node = ReadDirectory(pathToTempPar, nodeName);
        
        // Store a reference to the nodes in the container to dispose of them later, as they are not disposed properly
        var containerNode = node.GetFormatAs<NodeContainerFormat>();
        var par = NodeFactory.FromFile(pathToPar);
        
        par.TransformWith(typeof(ParArchiveReader), readerParams);
        
        Node searchResult = null;
        
        // Search for the par if it's not the actual loaded par
        if (par.Name != new FileInfo(parPathReal).Name) {
            searchResult = SearchParNode(par, parPathReal.ToLowerInvariant());
            
            if (searchResult == null) {
                // Create empty node to transfer the children to
                searchResult = NodeFactory.CreateContainer("empty");
                searchResult.Add(NodeFactory.CreateContainer("."));
            }
            
            // Swap the search result and its parent
            (par, searchResult) = (searchResult, par);
        }
        
        writerParams.IncludeDots = par.Children[0].Name == ".";
        writerParams.ResetFileDates = true;
        
        containerNode.MoveChildrenTo(writerParams.IncludeDots ? par.Children[0] : par, true);
        par.SortChildren((x, y) => string.CompareOrdinal(x.Name.ToLowerInvariant(), y.Name.ToLowerInvariant()));
        
        writerParams.IncludeDots = false;
        
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(pathToModPar))!);
        par.TransformWith(typeof(ParArchiveWriter), writerParams);
        
        par.Dispose();
        searchResult?.Dispose();
        node.Dispose();
        containerNode.Dispose();
        
        // Remove the .partemp directory
        DeleteDirectory(pathToTempPar);
        
        console.WriteLineIfVerbose();
        console.WriteLine($"Repacked {fileDict.Count} file(s) in {parPath + ".par"}!");
        
        return console;
    }
    
    private static List<string> GetModFiles(string par, string mod, ConsoleOutput console) {
        List<string> result;
        
        if (mod.StartsWith(Constants.PARLESS_NAME)) {
            // Get index of ".parless" in par path
            // 15 = ParlessMod.NAME.Length + 1
            result = GetModFiles(Path.Combine(GamePath.DataPath, par.Insert(int.Parse(mod[15..]) - 1, ".parless")), console);
        } else {
            result = GetModFiles(GamePath.GetModPathFromDataPath(mod, par), console);
        }
        
        // Get file path relative to par
        return result.Select(f => f.Replace(".parless", "")[(f.Replace(".parless", "").IndexOf(par, StringComparison.Ordinal) + par.Length + 1)..]).ToList();
    }
    
    private static List<string> GetModFiles(string path, ConsoleOutput console) {
        List<string> files = [];
        
        // Add files in current directory
        foreach (var p in Directory.GetFiles(path).Where(f => !f.EndsWith(Constants.VORTEX_MANAGED_FILE)).Select(GamePath.GetDataPathFrom)) {
            files.Add(p);
            console.WriteLineIfVerbose($"Adding file: {p}");
        }
        
        // Get files for all subdirectories
        foreach (var folder in Directory.GetDirectories(path)) {
            files.AddRange(GetModFiles(folder, console));
        }
        
        return files;
    }
    
    private static Node ReadDirectory(string dirPath, string nodeName = "") {
        dirPath = Path.GetFullPath(dirPath);
        
        if (string.IsNullOrEmpty(nodeName)) {
            nodeName = Path.GetFileName(dirPath);
        }
        
        var container = NodeFactory.CreateContainer(nodeName);
        var directoryInfo = new DirectoryInfo(dirPath);
        
        container.Tags["DirectoryInfo"] = directoryInfo;
        
        var files = directoryInfo.GetFiles();
        foreach (var file in files) {
            var fileNode = NodeFactory.FromFile(file.FullName);
            
            container.Add(fileNode);
        }
        
        var directories = directoryInfo.GetDirectories();
        foreach (var directory in directories) {
            var directoryNode = ReadDirectory(directory.FullName);
            
            container.Add(directoryNode);
        }
        
        return container;
    }
    
    private static Node SearchParNode(Node node, string path) {
        var paths = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        
        return paths.Length == 0 
            ? null 
            : SearchParNode(node, paths, 0);
    }
    
    private static Node SearchParNode(Node node, string[] paths, int pathIndex) {
        if (pathIndex >= paths.Length) {
            return node;
        }
        
        var path = paths[pathIndex];
        
        foreach (var child in node.Children) {
            if (child.Name == ".") {
                return SearchParNode(child, paths, pathIndex);
            }
            
            if (child.Name == path || child.Name == path + ".par") {
                return SearchParNode(child, paths, pathIndex + 1);
            }
        }
        
        return null;
    }
}

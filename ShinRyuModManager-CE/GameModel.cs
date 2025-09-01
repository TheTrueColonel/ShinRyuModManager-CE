using ParLibrary;
using ParLibrary.Converter;
using ShinRyuModManager.ModLoadOrder;
using Utils;
using Yarhl.FileSystem;

namespace ShinRyuModManager;

public static class GameModel {
    public static bool SupportsUBIK(Game game) {
        return game >= Game.LostJudgment && game != Game.Eve;
    }
    
    // Yakuza 5 is quirky in the sense that accessing the loose files won't be enough
    // Things have to be duplicated in places like this
    // 1) Folder in root hact folder (hact/h1000_my_cool_new_hact)
    // 2) Par in root hact folder (hact/h1000_my_cool_new_hact.par)
    // 3) Folder + par in new hact folder (hact/h1000_my_cool_new_hact/000   AND   hact/h1000_my_cool_new_hact/000.par)
    // This can seriously bloat mod size. Instead of making modders duplicate their hacts like this
    // Let's do this for them (because we are nice)
    public static void DoY5HActProcedure(MLO mlo) {
        var hasHacts = false;
        var hactDirs = new HashSet<string>();

        foreach (var file in mlo.Files) {
            if (!file.Name.Contains("/hact/") || file.Name.EndsWith(".par"))
                continue;

            var mod = mlo.Mods[file.Index];
            var filePath = Path.Combine("mods", mod, file.Name);
            var fileInfo = new FileInfo(filePath);
            // Get the folder from a path like so -> data/hact/h5000_some_hact/cmn/cmn.bin
            var hactDir = fileInfo.Directory.Parent.Name;

            if (fileInfo.Directory.Parent.Parent.Name == "hact") {
                var hactPath = fileInfo.Directory.Parent.FullName;
                    
                var parPath = Path.Combine(fileInfo.Directory.Parent.Parent.FullName, hactDir + ".par");
                    
                // Legacy hact mods
                if (File.Exists(parPath))
                    continue;
                    
                //Legacy hact mods
                if (!Directory.Exists(Path.Combine(hactPath, "000")) || !Directory.Exists(Path.Combine(hactPath, "cmn")))
                    continue;

                if (hactDirs.Contains(hactPath))
                    continue;

                if (GamePath.ExistsInDataAsPar(hactPath))
                    continue;

                hactDirs.Add(hactPath);
            }

            hasHacts = true;
        }
            
        if (!hasHacts)
            return;

        foreach (var hactDirPath in hactDirs) {
            var hactDir = new DirectoryInfo(hactDirPath);
            var parlessDir = new DirectoryInfo(Path.Combine(GamePath.ModsPath, "Parless", "hact", hactDir.Name));

            if (!parlessDir.Exists)
                parlessDir.Create();

            foreach (var dir in hactDir.GetDirectories()) {
                // We already repack ptc
                if (dir.Name == "ptc" && File.Exists(Path.Combine(hactDir.FullName, "ptc.par")))
                    continue;

                var outputPath = Path.Combine(parlessDir.FullName, $"{dir.Name}.par");
                Gibbed.Yakuza0.Pack.Program.Main([dir.FullName], outputPath);
            }
            
            Gibbed.Yakuza0.Pack.Program.Main([parlessDir.FullName], Path.Combine(parlessDir.Parent.FullName, $"{hactDir.Name}.par"));
        }
    }

    public static void DoOEHActProcedure(MLO mlo) {
        var hasHacts = false;
        var hactDirs = new HashSet<string>();

        foreach (var file in mlo.Files) {
            if (!file.Name.Contains("/hact/") || file.Name.EndsWith(".par"))
                continue;

            var mod = mlo.Mods[file.Index];
            var filePath = Path.Combine("mods", mod, file.Name);
            var fileInfo = new FileInfo(filePath);

            //get the folder from a path like this -> data/hact/h5000_some_hact/cmn/cmn.bin
            var hactDir = fileInfo.Directory.Parent.Name;

            if (fileInfo.Directory.Parent.Parent.Name == "hact") {
                var hactPath = fileInfo.Directory.Parent.FullName;

                var parPath = Path.Combine(fileInfo.Directory.Parent.Parent.FullName, hactDir + ".par");

                //Legacy hact mods
                if (File.Exists(parPath))
                    continue;

                //Legacy hact mods
                if (!Directory.Exists(Path.Combine(hactPath, "000")) || !Directory.Exists(Path.Combine(hactPath, "cmn")))
                    continue;

                if (hactDirs.Contains(hactPath))
                    continue;

                hactDirs.Add(hactPath);
            }

            hasHacts = true;
        }

        if (!hasHacts)
            return;

        foreach (var hactDirPath in hactDirs) {
            var hactDir = new DirectoryInfo(hactDirPath);
            var parlessDir = new DirectoryInfo(Path.Combine(GamePath.ModsPath, "Parless", "hact", hactDir.Name));

            if (!parlessDir.Exists)
                parlessDir.Create();

            foreach (var dir in hactDir.GetDirectories()) {
                //We already repack ptc 
                if (dir.Name == "ptc" /*&& File.Exists(Path.Combine(hactDir.FullName, "ptc.par"))*/)
                    continue;

                var outputFakeDir = Path.Combine(parlessDir.FullName, dir.Name);
               
                if(!Directory.Exists(outputFakeDir))
                    Directory.CreateDirectory(outputFakeDir);

                var outputPath = Path.Combine(parlessDir.FullName, dir.Name + ".par");
                
                Gibbed.Yakuza0.Pack.Program.Main([outputFakeDir], outputPath);
                
                try {
                    new DirectoryInfo(outputFakeDir).Delete(true);
                } catch {
                    // ignored
                }
            }

            Gibbed.Yakuza0.Pack.Program.Main([parlessDir.FullName], Path.Combine(parlessDir.Parent.FullName, hactDir.Name + ".par"));
        }
    }
    
    public static void DoDEHActProcedure(MLO mlo, string codename) {
        var hasHacts = false;
        var hactDirs = new HashSet<string>();

        foreach (var file in mlo.Files) {
            if (!file.Name.Contains($"/hact_{codename}/") || file.Name.EndsWith(".par"))
                continue;

            var mod = mlo.Mods[file.Index];
            var filePath = Path.Combine("mods", mod, file.Name);
            var fileInfo = new FileInfo(filePath);

            //get the folder from a path like this -> data/hact_yazawa/h5000_some_hact/cmn/cmn.bin
            var hactDir = fileInfo.Directory.Parent.Name;

            if (fileInfo.Directory.Parent.Parent.Name == $"hact_{codename}") {
                var hactPath = fileInfo.Directory.Parent.FullName;

                var parPath = Path.Combine(fileInfo.Directory.Parent.Parent.FullName, hactDir + ".par");

                //Legacy hact mods
                if (File.Exists(parPath))
                    continue;

                //Legacy hact mods
                if (!Directory.Exists(Path.Combine(hactPath, "000")) || !Directory.Exists(Path.Combine(hactPath, "cmn")))
                    continue;

                if (hactDirs.Contains(hactPath))
                    continue;

                var isVanillaHact = GamePath.ExistsInDataAsPar(fileInfo.Directory.Parent.FullName);

                if (isVanillaHact)
                    continue;

                hactDirs.Add(hactPath);
            }
            
            hasHacts = true;
        }

        if (!hasHacts)
            return;

        foreach (var hactDirPath in hactDirs) {
            var hactDir = new DirectoryInfo(hactDirPath);
            var parlessDir = new DirectoryInfo(Path.Combine(GamePath.ModsPath, "Parless", "hact_" + codename, hactDir.Name));

            if (!parlessDir.Exists)
                parlessDir.Create();

            foreach (var dir in hactDir.GetDirectories()) {
                //We already repack ptc 
                if (dir.Name == "ptc" && File.Exists(Path.Combine(hactDir.FullName, "ptc.par")))
                    continue;

                var outputPath = Path.Combine(parlessDir.FullName, dir.Name + ".par");
                Gibbed.Yakuza0.Pack.Program.Main([dir.FullName], outputPath);
            }

            Gibbed.Yakuza0.Pack.Program.Main([parlessDir.FullName], Path.Combine(parlessDir.Parent.FullName, hactDir.Name + ".par"));

            new DirectoryInfo(parlessDir.FullName).Delete(true);
        }
    }
    
    // Got to be a better way to do this...
    public static void DoUBIKProcedure(MLO mlo) {
        var charaPath = Path.Combine("data/chara.par");
        
        if (!File.Exists(charaPath))
            return;
        
        var hasUbiks = mlo.Files.Any(file => file.Name.EndsWith(".ubik"));
        
        if (!hasUbiks)
            return;
        
        var ubikDir = Path.Combine(Constants.PARLESS_MODS_PATH, "ubik");
        var par = NodeFactory.FromFile(charaPath, "par");
        
        par.TransformWith(typeof(ParArchiveReader), new ParArchiveReaderParameters { Recursive = true });
        
        var ubik = Navigator.IterateNodes(par).FirstOrDefault(x => x.Path.EndsWith("ubik"));
        
        if (!Directory.Exists(Constants.PARLESS_MODS_PATH))
            Directory.CreateDirectory(Constants.PARLESS_MODS_PATH);
        
        if (!Directory.Exists(ubikDir))
            Directory.CreateDirectory(ubikDir);
        
        foreach (var node in ubik!.Children) {
            var ubikFile = node.GetFormatAs<ParFile>();
            
            if (ubikFile.IsCompressed)
                node.TransformWith<ParLibrary.Sllz.Decompressor>();
            
            var filePath = Path.Combine(ubikDir, node.Name);
            
            if (node.Stream.Length > 0)
                node.Stream.WriteTo(filePath);
        }
        
        foreach (var file in mlo.Files) {
            if (!file.Name.EndsWith(".ubik"))
                continue;
            
            var path = Path.Combine("mods", mlo.Mods[file.Index] + file.Name);
            File.Copy(path, Path.Combine(ubikDir, Path.GetFileName(file.Name)), true);
        }
        
        par.Dispose();
    }
}

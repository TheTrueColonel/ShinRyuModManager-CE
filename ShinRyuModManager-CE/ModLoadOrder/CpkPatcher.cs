using Serilog;
using Utils;

namespace ShinRyuModManager.ModLoadOrder;

// Intended only for OE bgm/se.cpk
internal static class CpkPatcher {
    public static async Task RepackDictionary(Dictionary<string, List<string>> cpkDict) {
        if (cpkDict == null || cpkDict.Count <= 0) {
            return;
        }
        
        Log.Information("Repacking CPKs...");
        
        var cpkPath = Path.Combine(GamePath.ModsPath, "Parless");
        
        if (!Directory.Exists(cpkPath))
            Directory.CreateDirectory(cpkPath);
        
        foreach (var kvp in cpkDict) {
            var cpkDir = cpkPath + kvp.Key;

            string origCpk;

            if (!kvp.Key.Contains(".cpk")) {
                origCpk = GamePath.DataPath + kvp.Key + ".cpk";
            } else {
                origCpk = GamePath.DataPath + kvp.Key;
            }
            
            if (!Directory.Exists(cpkDir))
                Directory.CreateDirectory(cpkDir);
            
            foreach (var mod in kvp.Value) {
                var modCpkDir = Path.Combine(GamePath.ModsPath, mod).Replace(".cpk", "");
                var cpkFiles = Directory.GetFiles(modCpkDir, "*.");
                
                foreach (var file in cpkFiles) {
                    File.Copy(file, Path.Combine(cpkDir, Path.GetFileName(file)), true);
                }
            }
            
            CriPakTools.Program.Modify(origCpk, cpkDir, new DirectoryInfo(cpkDir).FullName + ".cpk");
        }
        
        await Task.CompletedTask;
    }
}

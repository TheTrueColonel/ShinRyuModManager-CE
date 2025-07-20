using Utils;

namespace ShinRyuModManager.ModLoadOrder;

// Intended only for OE bgm/se.cpk
internal static class CpkPatcher {
    public static async Task RepackDictionary(Dictionary<string, List<string>> cpkDict) {
        if (cpkDict == null || cpkDict.Count <= 0) {
            return;
        }
        
        Program.Log("Repacking CPKs...");
        
        var cpkPath = Path.Combine(GamePath.GetModsPath(), "Parless");
        
        if (!Directory.Exists(cpkPath))
            Directory.CreateDirectory(cpkPath);
        
        foreach (var kvp in cpkDict) {
            var cpkDir = cpkPath + kvp.Key;
            var origCpk = GamePath.GetDataPath() + kvp.Key + ".cpk";
            
            if (!Directory.Exists(cpkDir))
                Directory.CreateDirectory(cpkDir);
            
            foreach (var mod in kvp.Value) {
                var modCpkDir = Path.Combine(GamePath.GetModsPath(), mod);
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

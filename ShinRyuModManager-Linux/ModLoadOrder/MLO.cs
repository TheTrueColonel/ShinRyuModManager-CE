using ShinRyuModManager;
using Yarhl.IO;

namespace ShinRyuModManager.ModLoadOrder;

public class MLO {
    public const string MAGIC = "_OLM"; // MLO_ but in little endian, because that's how the yakuza works
    public const uint ENDIANNESS = 0x21; // Little endian
    public const uint VERSION = 0x020000; // 2.0
    public const uint FILESIZE = 0x0; // Remaining faithful to RGG by adding a filesize that's not used
    
    public readonly List<string> Mods;
    public readonly List<ParlessFile> Files;
    public readonly List<ParlessFolder> ParlessFolders;
    public readonly List<CpkFolder> CpkFolders;
    
    public MLO(List<int> modIndices, List<string> mods, SortedSet<string> fileSet, List<ParlessFolder> parlessFolders, Dictionary<string, List<int>> cpkFolders) {
        var files = fileSet.ToList();
        
        Mods = mods;
        Files = [];
        
        for (var i = 0; i < modIndices.Count - 1; i++) {
            for (var j = modIndices[i]; j < modIndices[i + 1]; j++) {
                var file = new ParlessFile(Utils.NormalizeNameLower(files[j]), i);
                
                Files.Add(file);
            }
        }
        
        ParlessFolders = parlessFolders.Select(f => f with { Name = Utils.NormalizeNameLower(f.Name) }).ToList();
        
        CpkFolders = cpkFolders
                     .Select(pair => new CpkFolder(Utils.NormalizeNameLower(pair.Key), pair.Value.Select(v => (ushort)v).ToList()))
                     .ToList();
    }
    
    public void WriteMLO(string path) {
        var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var dataStream = DataStreamFactory.FromStream(stream);
        
        var writer = new DataWriter(dataStream);
        
        // Write header
        writer.Write(MAGIC, false);
        writer.Write(ENDIANNESS);
        writer.Write(VERSION);
        writer.Write(FILESIZE);
        
        writer.Write(0x40); // Mods start (size of header)
        writer.WriteOfType(typeof(uint), Mods.Count);
        
        writer.Write(0); // Files start (to be written later)
        writer.WriteOfType(typeof(uint), Files.Count);
        
        writer.Write(0); // Parless folders start (to be written later)
        writer.WriteOfType(typeof(uint), ParlessFolders.Count);
        
        writer.Write(0); // Cpk folders start (to be written later)
        writer.WriteOfType(typeof(uint), CpkFolders.Count);
        
        writer.WriteTimes(0, 0x10); // Padding
        
        // 0x0: Length
        // 0x2: String
        foreach (var mod in Mods) {
            writer.WriteOfType(typeof(ushort), mod.Length + 1);
            writer.Write(mod);
        }
        
        var fileStartPos = writer.Stream.Position;
        
        // 0x0: Index of mod
        // 0x2: Length
        // 0x4: String
        foreach (var file in Files) {
            writer.WriteOfType(typeof(ushort), file.Index);
            writer.WriteOfType(typeof(ushort), file.Name.Length + 1);
            writer.Write(file.Name);
        }
        
        var parlessStartPos = writer.Stream.Position;
        
        // 0x0: Index of .parless in string
        // 0x2: Length
        // 0x4: String
        foreach (var folder in ParlessFolders) {
            writer.WriteOfType(typeof(ushort), folder.Index);
            writer.WriteOfType(typeof(ushort), folder.Name.Length + 1);
            writer.Write(folder.Name);
        }
        
        var cpkFolderStartPos = writer.Stream.Position;
        
        // 0x0: Mod Count
        // 0x2: Length
        // 0x4: String
        // 0x?: Mod Indices
        foreach (var folder in CpkFolders) {
            writer.WriteOfType(typeof(ushort), folder.Indices.Count);
            writer.WriteOfType(typeof(ushort), folder.Name.Length + 1);
            writer.Write(folder.Name);
            
            foreach (var index in folder.Indices) {
                writer.WriteOfType(typeof(ushort), index);
            }
        }
        
        // Write file size
        writer.Stream.Seek(0xC);
        writer.WriteOfType(typeof(uint), writer.Stream.Length);
        
        // Write file start position
        writer.Stream.Seek(0x18);
        writer.WriteOfType(typeof(uint), fileStartPos);
        
        // Write parless folders start position
        writer.Stream.Seek(0x20);
        writer.WriteOfType(typeof(uint), parlessStartPos);
        
        // Write cpk folders start position
        writer.Stream.Seek(0x28);
        writer.WriteOfType(typeof(uint), cpkFolderStartPos);
    }
}
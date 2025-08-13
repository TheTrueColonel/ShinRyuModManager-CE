using YamlDotNet.Serialization;

namespace ShinRyuModManager;

public class LibMeta {
    public Guid GUID { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public string Description { get; set; }
    public bool CanBeDisabled { get; set; }
    public bool IsDisabled { get; set; }
    /// <summary>
    /// A semicolon (;) separated list of game executable names.
    /// </summary>
    public string TargetGames { get; set; }
    public string Source { get; set; }
    public string Download { get; set; }
    public string MainBinary { get; set; }

    public static LibMeta ReadLibMeta(string yamlString) {
        var deserializer = new DeserializerBuilder().Build();
        
        return deserializer.Deserialize<LibMeta>(yamlString);
    }

    public static List<LibMeta> Fetch() {
        var yamlString = Utils.Client.GetStringAsync($"https://raw.githubusercontent.com/{Settings.LIBRARIES_INFO_REPO_OWNER}/{Settings.LIBRARIES_INFO_REPO}/main/{Settings.LIBRARIES_INFO_REPO_FILE_PATH}").GetAwaiter().GetResult();

        var localManifestCopyPath = Program.GetLocalLibraryCopyPath();

        if (!File.Exists(localManifestCopyPath) && !Utils.IsFileLocked(localManifestCopyPath)) {
            File.WriteAllText(localManifestCopyPath!, yamlString);
        }

        return ReadLibMetaManifest(yamlString);
    }

    public static List<LibMeta> ReadLibMetaManifest(string yamlString) {
        var returnList = new List<LibMeta>();

        var deserializer = new DeserializerBuilder().Build();
        var yamlObject = deserializer.Deserialize<Dictionary<string, LibMeta>>(yamlString);
        foreach (var key in yamlObject.Keys)
        {
            var meta = yamlObject[key];
            
            meta.GUID = new Guid(key);
            
            returnList.Add(meta);
        }

        Program.LibraryMetaCache = returnList;

        return returnList;
    }
}

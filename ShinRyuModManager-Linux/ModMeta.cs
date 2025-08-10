namespace ShinRyuModManager;

public class ModMeta {
    public string Name { get; internal set; }
    public string Author { get; internal init; }
    public string Version { get; internal init; }
    public string Description { get; internal init; }

    public static ModMeta GetPlaceholder() {
        return new ModMeta {
            Name = "Mod Name",
            Author = "Author",
            Version = "1.0.0",
            Description = "Mod description"
        };
    }

    public static ModMeta GetPlaceholder(string modName) {
        var meta = GetPlaceholder();

        meta.Name = modName;

        return meta;
    }
}

using Avalonia.Platform.Storage;

namespace ShinRyuModManager.UserInterface;

public static class CustomFilePickerFileTypes {
    public static FilePickerFileType CompressedZip { get; } = new("Zip Archive")
    {
        Patterns = ["*.zip"],
        AppleUniformTypeIdentifiers = ["public.zip-archive"],
        MimeTypes = ["application/zip"]
    };
    
    public static FilePickerFileType CompressedRar { get; } = new("Rar Archive")
    {
        Patterns = ["*.rar"],
        AppleUniformTypeIdentifiers = ["com.rarlab.rar-archive"],
        MimeTypes = ["application/x-rar-compressed", "application/vnd.rar", "application/x-rar"]
    };

    
    public static FilePickerFileType CompressedGzip { get; } = new("Gzip Archive")
    {
        Patterns = ["*.gz"],
        AppleUniformTypeIdentifiers = ["org.gnu.gnu-zip-archive"],
        MimeTypes = ["application/gzip"]
    };
    
    public static FilePickerFileType YamlFile { get; } = new("YAML File")
    {
        Patterns = ["*.yaml"],
        AppleUniformTypeIdentifiers = ["public.yaml"],
        MimeTypes = ["application/yaml"]
    };
}

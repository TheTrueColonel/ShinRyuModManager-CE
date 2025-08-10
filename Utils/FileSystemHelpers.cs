namespace Utils;

public enum ArchiveType {
    Unknown,
    Zip,
    Gzip
}

public static class FileSystemHelpers {
    public static ArchiveType DetectArchiveType(string path) {
        if (!File.Exists(path))
            return ArchiveType.Unknown;
        
        using var fs = File.OpenRead(path);
        var buffer = new byte[4];

        fs.ReadExactly(buffer);
        
        if (buffer[0] == 0x1F && buffer[1] == 0x8B) { // GZIP Header
            return ArchiveType.Gzip;
        } 
        
        if (buffer[0] == 0x50 && buffer[1] == 0x4B) { // ZIP Header
            return ArchiveType.Zip;
        }

        return ArchiveType.Unknown;
    }
}

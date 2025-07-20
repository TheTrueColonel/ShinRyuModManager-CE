namespace ShinRyuModManager;

public static class Utils {
    public static string NormalizeSeparator(string path) {
        return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
    }

    public static string NormalizeNameLower(string path) {
        return path.ToLowerInvariant().Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
    }
    
    internal static bool IsFileBlocked(string path) {
        if (!File.Exists(path))
            return false;
        
        try {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            return false;
        }
        catch (IOException) // File is in use
        {
            return true; 
        }
        catch (UnauthorizedAccessException) // Unable to access
        {
            return true;
        }
    }
}

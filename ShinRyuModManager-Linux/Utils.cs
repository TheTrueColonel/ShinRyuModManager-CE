using System.Text;

namespace ShinRyuModManager;

public static class Utils {

    public static string NormalizeNameLower(string path) {
        return NormalizeSeparator(path.ToLowerInvariant());
    }

    public static string NormalizeToNodePath(string path) {
        return NormalizeSeparator(path, '/');
    }
    
    internal static bool IsFileLocked(string path) {
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
    
    private static string NormalizeSeparator(string path, char? separator = null) {
        separator ??= Path.DirectorySeparatorChar;
        
        var sb = new StringBuilder(path.Length);

        foreach (var c in path) {
            sb.Append(c is '/' or '\\' 
                ? separator 
                : c);
        }

        return sb.ToString();
    }
}

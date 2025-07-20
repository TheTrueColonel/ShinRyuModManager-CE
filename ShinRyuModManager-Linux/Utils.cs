using System.Text;

namespace ShinRyuModManager;

public static class Utils {
    public static string NormalizeSeparator(string path) {
        var sb = new StringBuilder(path.Length);

        foreach (var c in path) {
            sb.Append(c is '/' or '\\' 
                ? Path.DirectorySeparatorChar 
                : c);
        }

        return sb.ToString();
    }

    public static string NormalizeNameLower(string path) {
        return NormalizeSeparator(path.ToLowerInvariant());
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

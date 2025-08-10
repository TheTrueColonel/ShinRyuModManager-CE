using System.Formats.Tar;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using Utils;

namespace ShinRyuModManager;

public static class Utils {
    public static string NormalizeNameLower(string path) {
        return NormalizeSeparator(path.ToLowerInvariant());
    }

    public static string NormalizeToNodePath(string path) {
        return NormalizeSeparator(path, '/');
    }

    public static string GetAppVersion() {
        return Assembly.GetExecutingAssembly().GetName().Version!.ToString();
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

    internal static async Task<bool> TryInstallModZipAsync(string path) {
        if (!File.Exists(path))
            return false;

        var archiveType = FileSystemHelpers.DetectArchiveType(path);

        if (archiveType == ArchiveType.Zip) {
            ZipFile.ExtractToDirectory(path, GamePath.ModsPath);
        } else if (archiveType == ArchiveType.Gzip) {
            await using var fs = File.OpenRead(path);
            await using var gz = new GZipStream(fs, CompressionMode.Decompress, true);

            await TarFile.ExtractToDirectoryAsync(gz, GamePath.ModsPath, true);
        }

        return true;
    }
}

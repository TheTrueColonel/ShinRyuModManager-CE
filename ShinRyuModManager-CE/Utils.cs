using System.Text;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using SharpCompress.Readers;
using Utils;
using Yarhl.FileSystem;

namespace ShinRyuModManager;

public static class Utils {
    // Static to be used through project
    public static HttpClient Client { get; } = new HttpClient();
    
    public static string NormalizeNameLower(string path) {
        return NormalizeSeparator(path.ToLowerInvariant());
    }

    public static string NormalizeToNodePath(string path) {
        return NormalizeSeparator(path, NodeSystem.PathSeparator[0]);
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
        
        await using var fs = File.OpenRead(path);

        // TEMP: Handle .7z files. Remove when SharpCompress fixes this issue.
        var is7Z = SevenZipArchive.IsSevenZipFile(fs);

        fs.Seek(0, SeekOrigin.Begin);
        
        if (is7Z) {
            return Extract7ZFile(fs);
        }
        
        using var reader = ReaderFactory.Open(fs);

        var options = new ExtractionOptions {
            ExtractFullPath = true,
            Overwrite = true
        };

        while (reader.MoveToNextEntry()) {
            if (!reader.Entry.IsDirectory) {
                reader.WriteEntryToDirectory(GamePath.ModsPath, options);
            }
        }

        return true;
    }

    // For some reason the SharpCompress doesn't handle .7z files automatically.
    // This looks to be a working workaround for the time being.
    private static bool Extract7ZFile(Stream stream) {
        using var archive = SevenZipArchive.Open(stream);
        using var reader = archive.ExtractAllEntries();
        
        reader.WriteAllToDirectory(GamePath.ModsPath, new ExtractionOptions {
            ExtractFullPath = true,
            Overwrite = true
        });

        return true;
    }
    
    /// <summary>
    /// Compares two versions and returns true if the target version is higher than the current one.
    /// </summary>
    /// <param name="versionTarget">Target version.</param>
    /// <param name="versionCurrent">Current version to compare against.</param>
    /// <returns>A boolean.</returns>
    internal static bool CompareVersionIsHigher(string versionTarget, string versionCurrent)
    {
        var v1 = new Version(versionTarget);
        var v2 = new Version(versionCurrent);
        
        switch (v1.CompareTo(v2))
        {
            case 0: //same
                return false;

            case 1: //target is higher
                return true;

            case -1: //target is lower
                return false;

            default:
                return false;
        }
    }
}

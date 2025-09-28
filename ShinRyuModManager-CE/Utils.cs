using System.Reflection;
using System.Text;
using SharpCompress.Common;
using SharpCompress.Readers;
using Utils;
using Yarhl.FileSystem;

namespace ShinRyuModManager;

public static class Utils {
    // Statically instantiated to be used through project
    public static readonly HttpClient Client = new HttpClient();
    
    public static string NormalizeNameLower(string path) {
        return NormalizeSeparator(path.ToLowerInvariant());
    }

    public static string NormalizeToNodePath(string path) {
        return NormalizeSeparator(path, NodeSystem.PathSeparator.ToCharArray()[0]);
    }

    public static string GetAppVersion() {
        return Assembly.GetExecutingAssembly().GetName().Version!.ToString();
    }

    internal static bool CheckFlag(string flagName) {
        var currentPath = Path.GetDirectoryName(Environment.CurrentDirectory);
        var flagFilePath = Path.Combine(currentPath, flagName);

        return File.Exists(flagFilePath);
    }

    internal static void CreateFlag(string flagName) {
        if (CheckFlag(flagName))
            return;

        var currentPath = Path.GetDirectoryName(Environment.CurrentDirectory);
        var flagFilePath = Path.Combine(currentPath, flagName);
            
        File.Create(flagFilePath);
        File.SetAttributes(flagFilePath, File.GetAttributes(flagFilePath) | FileAttributes.Hidden);
    }

    internal static void DeleteFlag(string flagName) {
        if (!CheckFlag(flagName))
            return;
        
        var currentPath = Path.GetDirectoryName(Environment.CurrentDirectory);
        var flagFilePath = Path.Combine(currentPath, flagName);
        
        File.Delete(flagFilePath);
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

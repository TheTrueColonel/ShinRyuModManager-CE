using System.Diagnostics;
using System.Reflection;

namespace Utils;

public static class AssemblyVersion {
    /// <summary>
    /// Gets the calling assembly version.
    /// </summary>
    /// <returns>A <see cref="string"/>.</returns>
    public static string GetVersion() {
        var version = Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        
        return version;
    }
}

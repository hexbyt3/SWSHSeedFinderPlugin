using System;

namespace SVSeedFinderPlugin;

/// <summary>
/// Version management for the SV Seed Finder Plugin
/// </summary>
public static class PluginVersion
{
    /// <summary>
    /// Gets the current plugin version
    /// </summary>
    public static Version? PluginAssemblyVersion => GetLoadedVersion("SVSeedFinderPlugin");

    /// <summary>
    /// Gets the currently loaded PKHeX.Core version
    /// </summary>
    public static Version? LoadedPKHeXVersion => GetLoadedVersion("PKHeX.Core");

    /// <summary>
    /// Checks if the plugin is compatible with the loaded PKHeX version
    /// </summary>
    /// <returns>True if compatible, false otherwise</returns>
    public static bool IsCompatible()
    {
        var loaded = LoadedPKHeXVersion;
        var plugin = PluginAssemblyVersion;

        if (loaded == null || plugin == null)
            return true; // Assume compatible if we can't detect versions

        // Check if major and minor versions match
        return loaded.Major == plugin.Major && loaded.Minor == plugin.Minor;
    }

    /// <summary>
    /// Gets a user-friendly compatibility message
    /// </summary>
    public static string GetCompatibilityMessage()
    {
        var loaded = LoadedPKHeXVersion;
        var plugin = PluginAssemblyVersion;

        if (loaded == null || plugin == null)
            return "Unable to detect version information.";

        if (IsCompatible())
            return $"Plugin v{plugin} - Compatible with PKHeX v{loaded}";

        return $"Version mismatch! Plugin v{plugin} may not be compatible with PKHeX v{loaded}";
    }

    /// <summary>
    /// Checks if there's a version mismatch that should show a warning
    /// </summary>
    public static bool HasVersionMismatch()
    {
        var loaded = LoadedPKHeXVersion;
        var plugin = PluginAssemblyVersion;

        if (loaded == null || plugin == null)
            return false; // Don't warn if we can't detect

        // Warn if major or minor versions differ
        return loaded.Major != plugin.Major || loaded.Minor != plugin.Minor;
    }

    private static Version? GetLoadedVersion(string assemblyName)
    {
        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assembly = Array.Find(assemblies, x => x.GetName().Name == assemblyName);
            return assembly?.GetName().Version;
        }
        catch
        {
            return null;
        }
    }
}

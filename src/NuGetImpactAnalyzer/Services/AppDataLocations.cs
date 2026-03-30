using System.IO;

namespace NuGetImpactAnalyzer.Services;

/// <summary>
/// Per-user local storage under <see cref="Environment.SpecialFolder.LocalApplicationData"/> (Windows best practice).
/// Install directory remains read-only-capable; mutable data is not written under <see cref="AppContext.BaseDirectory"/>.
/// </summary>
public static class AppDataLocations
{
    /// <summary>
    /// Root folder for JSON config, caches, credentials index, and master-password file.
    /// </summary>
    public static string DefaultLocalDataRoot() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NuGetImpactAnalyzer");

    /// <summary>
    /// Parent directory for on-disk Git repository clones.
    /// </summary>
    public static string DefaultRepositoriesRoot() => Path.Combine(DefaultLocalDataRoot(), "repos");

    /// <summary>
    /// One-time copy from pre–Windows-layout paths (under the app directory) into local app data.
    /// </summary>
    public static void TryMigrateLegacyLayoutOnce()
    {
        var root = DefaultLocalDataRoot();
        var marker = Path.Combine(root, ".migrated-from-legacy-v1");
        if (File.Exists(marker))
        {
            return;
        }

        Directory.CreateDirectory(root);
        var reposRoot = DefaultRepositoriesRoot();

        var legacyData = Path.Combine(AppContext.BaseDirectory, "Data");
        var legacyConfig = Path.Combine(AppContext.BaseDirectory, "config.json");

        if (Directory.Exists(legacyData))
        {
            foreach (var fileName in new[] { "userpreferences.json", "cache.json" })
            {
                var src = Path.Combine(legacyData, fileName);
                var dest = Path.Combine(root, fileName);
                TryCopyFileIfMissing(src, dest);
            }

            try
            {
                foreach (var dir in Directory.EnumerateDirectories(legacyData))
                {
                    var name = Path.GetFileName(dir);
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    var destDir = Path.Combine(reposRoot, name);
                    if (Directory.Exists(destDir))
                    {
                        continue;
                    }

                    Directory.CreateDirectory(reposRoot);
                    CopyDirectoryRecursive(dir, destDir);
                }
            }
            catch
            {
                // best-effort; user can re-clone
            }
        }

        TryCopyFileIfMissing(legacyConfig, Path.Combine(root, "config.json"));

        try
        {
            File.WriteAllText(marker, DateTimeOffset.UtcNow.ToString("o"));
        }
        catch
        {
            // ignore
        }
    }

    private static void TryCopyFileIfMissing(string sourcePath, string destPath)
    {
        try
        {
            if (!File.Exists(sourcePath) || File.Exists(destPath))
            {
                return;
            }

            var dir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.Copy(sourcePath, destPath, overwrite: false);
        }
        catch
        {
            // best-effort migration
        }
    }

    private static void CopyDirectoryRecursive(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var sub in Directory.GetDirectories(sourceDir))
        {
            CopyDirectoryRecursive(sub, Path.Combine(destDir, Path.GetFileName(sub)));
        }
    }
}

using System.IO;
using System.Text.Json;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

/// <inheritdoc />
public sealed class MasterPasswordFileStore : IMasterPasswordFileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;

    public MasterPasswordFileStore(string storageDirectory)
    {
        Directory.CreateDirectory(storageDirectory);
        _filePath = Path.Combine(storageDirectory, "master-password.json");
    }

    public static string DefaultStorageDirectory() => AppDataLocations.DefaultLocalDataRoot();

    /// <inheritdoc />
    public bool FileExists => File.Exists(_filePath);

    /// <inheritdoc />
    public MasterPasswordReadResult TryRead()
    {
        try
        {
            var json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<MasterPasswordFileData>(json);
            if (data is null
                || data.PasswordSalt.Length == 0
                || data.PasswordHash.Length == 0
                || data.KeySalt.Length == 0)
            {
                return MasterPasswordReadResult.Fail("Master password file is invalid.");
            }

            return MasterPasswordReadResult.Ok(data);
        }
        catch (FileNotFoundException)
        {
            return MasterPasswordReadResult.Fail("No master password file was found.");
        }
        catch (JsonException ex)
        {
            return MasterPasswordReadResult.Fail($"Master password file is corrupted: {ex.Message}");
        }
        catch (Exception ex)
        {
            return MasterPasswordReadResult.Fail($"Could not read master password file: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public void Write(MasterPasswordFileData data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    /// <inheritdoc />
    public bool TryDeleteFile(out string? error)
    {
        error = null;
        if (!File.Exists(_filePath))
        {
            return true;
        }

        try
        {
            File.Delete(_filePath);
            return true;
        }
        catch (Exception ex)
        {
            error = $"Could not delete master password file: {ex.Message}";
            return false;
        }
    }
}

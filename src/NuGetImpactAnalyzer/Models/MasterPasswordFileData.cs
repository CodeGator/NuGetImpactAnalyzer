using System.Text.Json.Serialization;

namespace NuGetImpactAnalyzer.Models;

/// <summary>Serialized master-password verification material (no secrets in cleartext).</summary>
public sealed class MasterPasswordFileData
{
    public const int CurrentVersion = 1;

    [JsonPropertyName("version")]
    public int Version { get; set; } = CurrentVersion;

    /// <summary>Base64 salt for password verification hash.</summary>
    [JsonPropertyName("passwordSalt")]
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>Base64 PBKDF2-SHA256 output used to verify the master password.</summary>
    [JsonPropertyName("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Base64 salt used when deriving the data-encryption key from the master password.</summary>
    [JsonPropertyName("keySalt")]
    public string KeySalt { get; set; } = string.Empty;
}

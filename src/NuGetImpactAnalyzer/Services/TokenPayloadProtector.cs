using System.Security.Cryptography;
using System.Text;

namespace NuGetImpactAnalyzer.Services;

/// <summary>
/// AES-256-GCM wrapper for PAT strings stored in Credential Manager, prefixed with <c>v1:</c>.
/// </summary>
internal static class TokenPayloadProtector
{
    private const string Prefix = "v1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public static bool LooksLikeProtectedPayload(string? value) =>
        !string.IsNullOrEmpty(value) && value.StartsWith(Prefix, StringComparison.Ordinal);

    public static string Protect(string plaintext, ReadOnlySpan<byte> key32)
    {
        if (key32.Length != 32)
        {
            throw new ArgumentException("Key must be 32 bytes.", nameof(key32));
        }

        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plainBytes.Length];
        var tag = new byte[TagSize];
        using (var aes = new AesGcm(key32, TagSize))
        {
            aes.Encrypt(nonce, plainBytes, ciphertext, tag);
        }

        var packed = new byte[nonce.Length + tag.Length + ciphertext.Length];
        nonce.CopyTo(packed.AsSpan());
        tag.CopyTo(packed.AsSpan(NonceSize));
        ciphertext.CopyTo(packed.AsSpan(NonceSize + TagSize));

        return Prefix + Convert.ToBase64String(packed);
    }

    /// <summary>Returns plaintext, or null if not a valid protected payload.</summary>
    public static string? TryUnprotect(string stored, ReadOnlySpan<byte> key32)
    {
        if (key32.Length != 32 || !LooksLikeProtectedPayload(stored))
        {
            return null;
        }

        try
        {
            var packed = Convert.FromBase64String(stored[Prefix.Length..]);
            if (packed.Length < NonceSize + TagSize + 1)
            {
                return null;
            }

            var nonce = packed.AsSpan(0, NonceSize);
            var tag = packed.AsSpan(NonceSize, TagSize);
            var ciphertext = packed.AsSpan(NonceSize + TagSize);
            var plaintext = new byte[ciphertext.Length];
            using (var aes = new AesGcm(key32, TagSize))
            {
                aes.Decrypt(nonce, ciphertext, tag, plaintext);
            }

            return Encoding.UTF8.GetString(plaintext);
        }
        catch
        {
            return null;
        }
    }
}

using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace NuGetImpactAnalyzer.Services;

/// <summary>
/// Direct Windows Credential Manager access (no encryption). Used by <see cref="ProtectedCredentialService"/>.
/// </summary>
public sealed class WindowsCredentialStore
{
    internal const string TargetPrefix = "NuGetImpactAnalyzer:repo:";

    public void SavePassword(string repoName, string passwordValue)
    {
        if (string.IsNullOrWhiteSpace(repoName))
        {
            throw new ArgumentException("Repository name is required.", nameof(repoName));
        }

        if (string.IsNullOrEmpty(passwordValue))
        {
            Delete(repoName);
            return;
        }

        var target = BuildTarget(repoName);
        Win32CredentialApi.WriteGenericCredential(
            target,
            "PersonalAccessToken",
            passwordValue,
            Win32CredentialApi.CredPersistLocalMachine);
    }

    public string? GetPassword(string repoName)
    {
        if (string.IsNullOrWhiteSpace(repoName))
        {
            return null;
        }

        return Win32CredentialApi.TryReadGenericPassword(BuildTarget(repoName));
    }

    public void Delete(string repoName)
    {
        if (string.IsNullOrWhiteSpace(repoName))
        {
            return;
        }

        Win32CredentialApi.DeleteGenericCredential(BuildTarget(repoName));
    }

    public static string BuildTarget(string repoName)
    {
        var safe = SanitizeRepoNameForTarget(repoName.Trim());
        return TargetPrefix + safe;
    }

    private static string SanitizeRepoNameForTarget(string name)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat(new[] { ':', '*' }).ToHashSet();
        var chars = name.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (invalid.Contains(chars[i]))
            {
                chars[i] = '_';
            }
        }

        return new string(chars);
    }
}

file static class Win32CredentialApi
{
    internal const uint CredTypeGeneric = 1;
    internal const uint CredPersistLocalMachine = 2;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CredCredential
    {
        public uint Flags;
        public uint Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite(ref CredCredential credential, uint flags);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, uint type, uint reservedFlag, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(string target, uint type, uint reservedFlag);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CredFree(IntPtr cred);

    internal static void WriteGenericCredential(string target, string userName, string password, uint persist)
    {
        var secret = Encoding.Unicode.GetBytes(password);
        var targetPtr = IntPtr.Zero;
        var userPtr = IntPtr.Zero;
        var secretPtr = IntPtr.Zero;

        try
        {
            targetPtr = Marshal.StringToCoTaskMemUni(target);
            userPtr = Marshal.StringToCoTaskMemUni(userName);
            secretPtr = Marshal.AllocCoTaskMem(secret.Length);
            if (secret.Length > 0)
            {
                Marshal.Copy(secret, 0, secretPtr, secret.Length);
            }

            var cred = new CredCredential
            {
                Flags = 0,
                Type = CredTypeGeneric,
                TargetName = targetPtr,
                UserName = userPtr,
                Comment = IntPtr.Zero,
                LastWritten = default,
                CredentialBlob = secretPtr,
                CredentialBlobSize = (uint)secret.Length,
                Persist = persist,
                AttributeCount = 0,
                Attributes = IntPtr.Zero,
                TargetAlias = IntPtr.Zero,
            };

            if (!CredWrite(ref cred, 0))
            {
                throw new InvalidOperationException("Could not save credentials to Windows Credential Manager.");
            }
        }
        finally
        {
            if (targetPtr != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(targetPtr);
            }

            if (userPtr != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(userPtr);
            }

            if (secretPtr != IntPtr.Zero)
            {
                for (var i = 0; i < secret.Length; i++)
                {
                    Marshal.WriteByte(secretPtr, i, 0);
                }

                Marshal.FreeCoTaskMem(secretPtr);
            }
        }
    }

    internal static string? TryReadGenericPassword(string target)
    {
        if (!CredRead(target, CredTypeGeneric, 0, out var credPtr) || credPtr == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var cred = Marshal.PtrToStructure<CredCredential>(credPtr);
            if (cred.CredentialBlobSize == 0 || cred.CredentialBlob == IntPtr.Zero)
            {
                return string.Empty;
            }

            var len = (int)cred.CredentialBlobSize;
            var bytes = new byte[len];
            Marshal.Copy(cred.CredentialBlob, bytes, 0, len);
            var s = Encoding.Unicode.GetString(bytes);

            // Match common CredWrite blobs that included a trailing UTF-16 NUL
            var end = s.Length;
            while (end > 0 && s[end - 1] == '\0')
            {
                end--;
            }

            return end == s.Length ? s : s[..end];
        }
        finally
        {
            CredFree(credPtr);
        }
    }

    internal static void DeleteGenericCredential(string target)
    {
        CredDelete(target, CredTypeGeneric, 0);
    }
}

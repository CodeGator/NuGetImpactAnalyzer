namespace NuGetImpactAnalyzer.Models;

/// <summary>Material for a new master password: persisted file payload and in-memory token protection key.</summary>
public sealed record NewMasterPasswordSecrets(MasterPasswordFileData FilePayload, byte[] TokenProtectionKey);

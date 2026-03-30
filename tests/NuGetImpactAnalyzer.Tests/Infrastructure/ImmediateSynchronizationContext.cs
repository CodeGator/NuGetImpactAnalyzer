namespace NuGetImpactAnalyzer.Tests.Infrastructure;

/// <summary>
/// Runs <see cref="SynchronizationContext.Post"/> callbacks synchronously so <see cref="IProgress{T}"/>
/// handlers execute before control returns (deterministic tests).
/// </summary>
public sealed class ImmediateSynchronizationContext : SynchronizationContext
{
    public override void Post(SendOrPostCallback d, object? state) => d(state);

    public override void Send(SendOrPostCallback d, object? state) => d(state);
}

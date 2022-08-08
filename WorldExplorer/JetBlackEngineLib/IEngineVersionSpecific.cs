namespace JetBlackEngineLib;

/// <summary>
/// Represents a service that supports specific versions of the Jet Black engine.
/// </summary>
public interface IEngineVersionSpecific
{
    IReadOnlyList<EngineVersion> SupportedVersions { get; }
}
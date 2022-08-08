namespace JetBlackEngineLib.Data.Animation;

/// <summary>
/// Represents a decoder for a .anm file.
/// </summary>
public interface IAnmDecoder : ISupportsSpecificEngineVersions
{
    AnimData Decode(ReadOnlySpan<byte> data);
}
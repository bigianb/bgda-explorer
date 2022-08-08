using JetBlackEngineLib.Data;

namespace JetBlackEngineLib.Io.Animation;

/// <summary>
/// Represents a decoder for a .anm file.
/// </summary>
public interface IAnmDecoder : IEngineVersionSpecific
{
    AnimData Decode(ReadOnlySpan<byte> data);
}
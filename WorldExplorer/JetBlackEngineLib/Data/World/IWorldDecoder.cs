namespace JetBlackEngineLib.Data.World;

public interface IWorldDecoder : ISupportsSpecificEngineVersions
{
    WorldData Decode(ReadOnlySpan<byte> data, WorldTexFile? texFile);
}
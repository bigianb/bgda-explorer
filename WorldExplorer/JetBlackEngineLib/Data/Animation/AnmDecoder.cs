namespace JetBlackEngineLib.Data.Animation;

public abstract class AnmDecoder : IAnmDecoder
{
    public abstract IReadOnlyList<EngineVersion> SupportedVersions { get; }
        
    public abstract AnimData Decode(ReadOnlySpan<byte> data);

    public static AnimData Decode(EngineVersion engineVersion, ReadOnlySpan<byte> data)
    {
        var decoder = GetDecoderForVersion(engineVersion);
        return decoder.Decode(data);
    }

    public static IAnmDecoder GetDecoderForVersion(EngineVersion engineVersion)
    {
        return engineVersion switch
        {
            EngineVersion.DarkAlliance => new BdgaAnmDecoder(),
            EngineVersion.ReturnToArms => new RtaAnmDecoder(),
            EngineVersion.JusticeLeagueHeroes => new RtaAnmDecoder(),
            _ => throw new EngineNotSupportedException(engineVersion)
        };
    }
}
using System;
using WorldExplorer.DataModel;

namespace WorldExplorer.DataLoaders.Animation
{
    public abstract class AnmDecoder
    {
        public abstract AnimData Decode(ReadOnlySpan<byte> data);

        public static AnimData Decode(EngineVersion engineVersion, ReadOnlySpan<byte> data)
        {
            AnmDecoder decoder = engineVersion switch
            {
                EngineVersion.DarkAlliance => new BdgaAnmDecoder(),
                EngineVersion.ReturnToArms => new RtaAnmDecoder(),
                EngineVersion.JusticeLeagueHeroes => new RtaAnmDecoder(),
                _ => throw new EngineNotSupportedException(engineVersion)
            };
            return decoder.Decode(data);
        }
    }
}
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace JetBlackEngineLib.Data.World;

public class WorldFileV2Decoder : WorldFileDecoder
{
    private static readonly EngineVersion[] StaticSupportedVersions =
    {
        EngineVersion.ReturnToArms, EngineVersion.JusticeLeagueHeroes
    };
    
    public override IReadOnlyList<EngineVersion> SupportedVersions => StaticSupportedVersions;
    
    protected override IEnumerable<WorldElement> ReadElements(ReadOnlySpan<byte> data, WorldFileHeader header)
    {
        return IterateElements<WorldV2Element>(data, header, WorldV2Element.Size, RawDataToElement);
    }

    protected override WriteableBitmap? GetElementTexture(WorldElementDataInfo dataInfo, WorldTexFile texFile,
        WorldData worldData)
    {
        return texFile.GetBitmapRTA(dataInfo);
    }
        
    protected override int[,] ReadTextureChunkOffsets(ReadOnlySpan<byte> data, int offset, int x1, int y1,
        int x2, int y2)
    {
        var chunkOffsets = new int[100, 100];
        for (var y = y1; y <= y2; ++y)
        for (var x = x1; x <= x2; ++x)
        {
            // TODO: Figure out what this should really be
            int address = 0x800;
            chunkOffsets[y, x] = address;
        }

        return chunkOffsets;
    }
        
    private WorldElement? RawDataToElement(WorldV2Element rawEl, int elementIdx)
    {
        // Don't include things without textures
        if (rawEl.TexCellXY == 0)
        {
            return null;
        }
            
        WorldElement element = new()
        {
            ElementIndex = elementIdx,
            Position = rawEl.Pos / 16.0,
            BoundingBox = new Rect3D(rawEl.Bounds1, rawEl.Bounds2 - rawEl.Bounds1),
            NegYaxis = (rawEl.UnknownFlag36 & 0x40) == 0x40, // Just guessing
            XyzRotFlags = rawEl.RotFlags,
            UsesRotFlags = true,
            DataInfo = new WorldElementDataInfo
            {
                TextureMod = rawEl.TexCellXY % 100,
                TextureDiv = rawEl.TexCellXY / 100,
                TextureNumber = rawEl.TextureNum / 64,
                VifDataOffset = rawEl.VifDataOffset,
                VifDataLength = rawEl.VifLength,
            },
        };
        return element;
    }
}
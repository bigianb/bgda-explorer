using JetBlackEngineLib.Io;
using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using WorldExplorer.DataModel;

namespace WorldExplorer.DataLoaders.World
{
    public class WorldFileV1Decoder : WorldFileDecoder
    {
        protected override IEnumerable<WorldElement> ReadElements(ReadOnlySpan<byte> data, WorldFileHeader header)
        {
            return IterateElements<WorldV1Element>(data, header, WorldV1Element.Size, RawDataToElement);
        }

        protected override WriteableBitmap? GetElementTexture(WorldElementDataInfo dataInfo, WorldTexFile texFile,
            WorldData worldData)
        {
            return texFile.GetBitmapBGDA(dataInfo, worldData);
        }
        
        protected override int[,] ReadTextureChunkOffsets(ReadOnlySpan<byte> data, int offset, int x1, int y1, int x2, int y2)
        {
            var chunkOffsets = new int[100, 100];
            for (var y = y1; y <= y2; ++y)
            for (var x = x1; x <= x2; ++x)
            {
                var cellOffset = (((y - y1) * 100) + x - x1) * 8;
                // This test is needed to deal with town.world in BGDA which addresses textures outside of the maximum x range.
                if (data.Length >= offset + cellOffset + 4)
                {
                    var address = DataUtil.GetLeInt(data, offset + cellOffset);
                    chunkOffsets[y, x] = address;
                }
            }

            return chunkOffsets;
        }

        private WorldElement RawDataToElement(WorldV1Element rawEl, int elementIdx)
        {
            WorldElement element = new()
            {
                ElementIndex = elementIdx,
                Position = rawEl.Pos / 16.0,
                BoundingBox = new Rect3D(rawEl.Bounds1, rawEl.Bounds2 - rawEl.Bounds1),
                NegYaxis = (rawEl.Flags & 0x40) == 0x40,
                UsesRotFlags = (rawEl.Flags & 0x01) != 0,
                DataInfo = new WorldElementDataInfo
                {
                    TextureMod = rawEl.TexCellXY % 100,
                    TextureDiv = rawEl.TexCellXY / 100,
                    TextureNumber = rawEl.TextureNum / 64,
                    VifDataOffset = rawEl.VifDataOffset,
                    VifDataLength = rawEl.VifLength,
                },
                RawFlags = rawEl.Flags & 0xFFFF
            };

            if (element.UsesRotFlags)
            {
                element.XyzRotFlags = (rawEl.Flags >> 16) & 7;
            }
            else
            {
                element.CosAlpha = (rawEl.Flags >> 16) / 32767.0;
                element.SinAlpha = rawEl.SinAlpha / 32767.0;
            }

            return element;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using WorldExplorer.DataModel;
using WorldExplorer.Logging;

namespace WorldExplorer.DataLoaders
{
    public class WorldFileV2Decoder : WorldFileDecoder
    {
        protected override WorldElement ReadWorldElement(EngineVersion engineVersion, WorldTexFile texFile, ILogger log, byte[] data, int startOffset, WorldData worldData, DataReader reader, int elementArrayStart, int texX0, int texY0, int elementIdx)
        {
            var element = new WorldElement()
            {
                ElementIndex = elementIdx,
            };

            reader.SetOffset(elementArrayStart + elementIdx * 0x3C);

            var vifDataOffset = reader.ReadInt32();
            var vifLen = reader.ReadInt32();
            var x1 = reader.ReadFloat();
            var y1 = reader.ReadFloat();
            var z1 = reader.ReadFloat();
            var x2 = reader.ReadFloat();
            var y2 = reader.ReadFloat();
            var z2 = reader.ReadFloat();
            var textureNum = reader.ReadInt32() / 64;
            var unkFlag = reader.ReadInt32();
            var posx = reader.ReadInt32();
            var posy = reader.ReadInt32();
            var posz = reader.ReadInt32();
            var texCellxy = reader.ReadInt16();
            var rotFlags = reader.ReadInt32();
            var unknown2 = reader.ReadInt16(); // Doesn't seem to do anything

            // Don't include things without textures
            if (texCellxy == 0) return null;

            element.boundingBox = new Rect3D(x1, y1, z1, x2 - x1, y2 - y1, z2 - z1);
            var texX = texCellxy / 100;
            var texY = texCellxy % 100;

            if (textureNum != 0 && texFile != null)
            {
                element.Texture = texFile.GetBitmapRTA(texY, texX, textureNum);
            }

            var vifLogger = new StringLogger();
            var texWidth = 100;
            var texHeight = 100;

            if (element.Texture != null)
            {
                texWidth = element.Texture.PixelWidth;
                texHeight = element.Texture.PixelHeight;
            }

            var nregs = data[startOffset + vifDataOffset + 0x10];
            var vifStartOffset = (nregs + 2) * 0x10;
            element.VifDataOffset = startOffset + vifDataOffset + vifStartOffset;
            element.VifDataLength = vifLen * 0x10 - vifStartOffset;
            element.model = DecodeModel(engineVersion, vifLogger, data, startOffset + vifDataOffset + vifStartOffset, vifLen * 0x10 - vifStartOffset, texWidth, texHeight);
            element.pos = new Vector3D(posx / 16.0, posy / 16.0, posz / 16.0);
            element.negYaxis = (unkFlag & 0x40) == 0x40; // Just guessing
            element.xyzRotFlags = rotFlags;
            element.usesRotFlags = true;

            log.LogLine("-----------");
            log.LogLine("vifdata: " + vifDataOffset + ", " + vifLen);
            log.LogLine("Bounding Box: " + element.boundingBox.ToString());
            log.LogLine("Texture Num: " + textureNum);
            log.LogLine("unkFlag: " + unkFlag);
            log.LogLine("Raw Position : " + posx + ", " + posy + ", " + posz);
            log.LogLine("Flags: " + rotFlags);
            log.LogLine("Unknown2: " + unknown2);
            log.LogLine("Scaled Position : " + element.pos.X + ", " + element.pos.Y + ", " + element.pos.Z);

            return element;
        }
    }
}

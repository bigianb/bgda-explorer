using System;
using System.Windows.Media.Media3D;
using WorldExplorer.DataModel;
using WorldExplorer.Logging;

namespace WorldExplorer.DataLoaders
{
    public class WorldFileV1Decoder : WorldFileDecoder
    {
        protected override WorldElement ReadWorldElement(EngineVersion engineVersion, WorldTexFile texFile, ILogger log, byte[] data, int startOffset, WorldData worldData, DataReader reader, int elementArrayStart, int texX0, int texY0, int elementIdx)
        {
            var element = new WorldElement();

            reader.SetOffset(elementArrayStart + elementIdx * 0x38);

            var vifDataOffset = reader.ReadInt32();

            var tex2 = reader.ReadInt32();
            var vifLen = reader.ReadInt32();

            var x1 = reader.ReadFloat();
            var y1 = reader.ReadFloat();
            var z1 = reader.ReadFloat();
            var x2 = reader.ReadFloat();
            var y2 = reader.ReadFloat();
            var z2 = reader.ReadFloat();
            element.boundingBox = new Rect3D(x1, y1, z1, x2 - x1, y2 - y1, z2 - z1);

            var textureNum = reader.ReadInt32() / 0x40;
            int texCellxy = reader.ReadInt16();
            var y = texCellxy / 100;
            var x = texCellxy % 100;

            if (textureNum != 0)
            {
                element.Texture = texFile.GetBitmap(worldData.textureChunkOffsets[y, x], textureNum);
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

            int posx = reader.ReadInt16();
            int posy = reader.ReadInt16();
            int posz = reader.ReadInt16();

            element.pos = new Vector3D(posx / 16.0, posy / 16.0, posz / 16.0);

            var flags = reader.ReadInt32();

            log.LogLine("-----------");
            if (tex2 != 0)
            {
                log.LogLine("Tex2=" + tex2);
            }
            log.LogLine("vifdata: " + vifDataOffset + ", " + vifLen);
            log.LogLine("Bounding Box: " + element.boundingBox.ToString());
            log.LogLine("Texture Num: " + textureNum);
            if (element.Texture != null)
            {
                log.LogLine("Found in texture chunk: " + x + ", " + y);
            }
            log.LogLine("Position : " + posx + ", " + posy + ", " + posz);

            if ((flags & 0x01) == 0)
            {
                element.cosAlpha = (flags >> 16) / 32767.0;
                element.sinAlpha = reader.ReadInt16() / 32767.0;
                element.usesRotFlags = false;
                log.LogLine("Flags   : " + HexUtil.formatHexUShort(flags & 0xFFFF));
                log.LogLine("cos alpha : " + element.cosAlpha);
                log.LogLine("sin alpha : " + element.sinAlpha);
                log.LogLine("alpha(cos, sin): " + Math.Acos(element.cosAlpha) * 180.0 / Math.PI + ", " + Math.Asin(element.sinAlpha) * 180.0 / Math.PI);
            }
            else
            {
                reader.ReadInt16();     // not necessary but makes the code more obvious.
                element.xyzRotFlags = (flags >> 16) & 7;
                element.usesRotFlags = true;
                log.LogLine("Flags   : " + HexUtil.formatHex(flags));
                log.LogLine("Rot Flags   : " + element.xyzRotFlags);
            }

            element.negYaxis = (flags & 0x40) == 0x40;
            worldData.worldElements.Add(element);

            return element;
        }
    }
}

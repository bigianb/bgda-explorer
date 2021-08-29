using System;
using System.Collections.Generic;
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
            if (tex2 != 0)
            {
                log.LogLine("Tex2=" + tex2);
            }

            var vifLen = reader.ReadInt32();
            log.LogLine("-----------");
            log.LogLine("vifdata: " + vifDataOffset + ", " + vifLen);

            var x1 = reader.ReadFloat();
            var y1 = reader.ReadFloat();
            var z1 = reader.ReadFloat();
            var x2 = reader.ReadFloat();
            var y2 = reader.ReadFloat();
            var z2 = reader.ReadFloat();

            element.boundingBox = new Rect3D(x1, y1, z1, x2 - x1, y2 - y1, z2 - z1);

            log.LogLine("Bounding Box: " + element.boundingBox.ToString());

            var textureNum = reader.ReadInt32() / 0x40;
            log.LogLine("Texture Num: " + textureNum);

            int texCellxy = reader.ReadInt16();
            var y = texCellxy / 100;
            var x = texCellxy % 100;

            if (EngineVersion.ReturnToArms == engineVersion || EngineVersion.JusticeLeagueHeroes == engineVersion)
            {
                x += texX0;
                y += texY0;
            }

            if (textureNum != 0)
            {
                if (EngineVersion.ReturnToArms == engineVersion || EngineVersion.JusticeLeagueHeroes == engineVersion)
                {
                    element.Texture = texFile.GetBitmapRTA(x, y, textureNum);
                }
                else
                {
                    element.Texture = texFile.GetBitmap(worldData.textureChunkOffsets[y, x], textureNum);
                }
            }

            if (element.Texture != null)
            {
                log.LogLine("Found in texture chunk: " + x + ", " + y);
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

            if (EngineVersion.ReturnToArms == engineVersion || EngineVersion.JusticeLeagueHeroes == engineVersion)
            {
                int unk = reader.ReadInt16();
                log.LogLine("Unknown: " + unk);
            }

            int posx = reader.ReadInt16();
            int posy = reader.ReadInt16();
            int posz = reader.ReadInt16();

            log.LogLine("Position : " + posx + ", " + posy + ", " + posz);

            element.pos = new Vector3D(posx / 16.0, posy / 16.0, posz / 16.0);

            if (EngineVersion.ReturnToArms == engineVersion || EngineVersion.JusticeLeagueHeroes == engineVersion)
            {
                // Just a guess, maybe wrong.
                element.pos = new Vector3D(posx / 16.0, posz / 16.0, posy / 16.0);
            }

            // I don't think RTA uses this flags scheme. From the data it looks like there are
            // 2 shorts (or possibly floats) following.

            var flags = reader.ReadInt32();

            if ((flags & 0x01) == 0)
            {
                log.LogLine("Flags   : " + HexUtil.formatHexUShort(flags & 0xFFFF));
                element.cosAlpha = (flags >> 16) / 32767.0;
                element.sinAlpha = reader.ReadInt16() / 32767.0;
                log.LogLine("cos alpha : " + element.cosAlpha);
                log.LogLine("sin alpha : " + element.sinAlpha);
                log.LogLine("alpha(cos, sin): " + Math.Acos(element.cosAlpha) * 180.0 / Math.PI + ", " + Math.Asin(element.sinAlpha) * 180.0 / Math.PI);

                element.usesRotFlags = false;
            }
            else
            {
                reader.ReadInt16();     // not necessary but makes the code more obvious.
                log.LogLine("Flags   : " + HexUtil.formatHex(flags));
                element.xyzRotFlags = (flags >> 16) & 7;
                element.usesRotFlags = true;
                log.LogLine("Rot Flags   : " + element.xyzRotFlags);
            }

            element.negYaxis = (flags & 0x40) == 0x40;

            if (EngineVersion.ReturnToArms == engineVersion || EngineVersion.JusticeLeagueHeroes == engineVersion)
            {
                flags = 0;
                element.usesRotFlags = true;
                log.LogLine("Forcing flags to 0 until we know the format better");
            }

            worldData.worldElements.Add(element);


            return element;
        }
    }
}

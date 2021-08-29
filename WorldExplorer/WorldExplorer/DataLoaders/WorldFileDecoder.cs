/*  Copyright (C) 2012 Ian Brown

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using WorldExplorer.DataModel;
using WorldExplorer.Logging;

namespace WorldExplorer.DataLoaders
{
    public class WorldFileDecoder
    {
        public WorldData Decode(EngineVersion engineVersion, WorldTexFile texFile, ILogger log, byte[] data, int startOffset, int length)
        {
            var worldData = new WorldData();

            var reader = new DataReader(data, startOffset, length);

            var numElements = reader.ReadInt32();       // 0

            reader.Skip(12); // Skipping 3 ints

            var numCols = reader.ReadInt32();           // x10        
            var numRows = reader.ReadInt32();           // x14        

            reader.Skip(12); // Skipping 3 ints         // x18 x1c x20
            var elementArrayStart = reader.ReadInt32(); // x24

            reader.Skip(8); // Skipping 2 ints
            var off38Cols = reader.ReadInt32();
            var off38Rows = reader.ReadInt32();
            var off38 = reader.ReadInt32();

            reader.Skip(28);
            var texll = reader.ReadInt32();
            var texur = reader.ReadInt32();
            var texX0 = texll % 100;
            var texY0 = texll / 100;
            var texX1 = texur % 100;
            var texY1 = texur / 100;

            reader.Skip(4);
            var worldTexOffsetsOffset = reader.ReadInt32();
            worldData.textureChunkOffsets = readTextureChunkOffsets(engineVersion, data, startOffset + worldTexOffsetsOffset, texX0, texY0, texX1 + 1, texY1);
            worldData.worldElements = new List<WorldElement>(numElements);

            for (var elementIdx = 0; elementIdx < numElements; ++elementIdx)
            {
                var element = EngineVersion.ReturnToArms == engineVersion || EngineVersion.JusticeLeagueHeroes == engineVersion
                    ? ReadWorldElementV2(engineVersion, texFile, log, data, startOffset, worldData, reader, elementArrayStart, texX0, texY0, elementIdx)
                    : ReadWorldElementV1(engineVersion, texFile, log, data, startOffset, worldData, reader, elementArrayStart, texX0, texY0, elementIdx);
                if (element != null)
                    worldData.worldElements.Add(element);
            }

            return worldData;
        }

        private WorldElement ReadWorldElementV2(EngineVersion engineVersion, WorldTexFile texFile, ILogger log, byte[] data, int startOffset, WorldData worldData, DataReader reader, int elementArrayStart, int texX0, int texY0, int elementIdx)
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
            element.model = decodeModel(engineVersion, vifLogger, data, startOffset + vifDataOffset + vifStartOffset, vifLen * 0x10 - vifStartOffset, texWidth, texHeight);
            element.pos = new Vector3D(posx / 16.0, posy / 16.0, posz / 16.0);

            //if ((flags & 0x01) == 0)
            //{
            //    log.LogLine("Flags   : " + HexUtil.formatHexUShort(flags & 0xFFFF));
            //    element.cosAlpha = (flags >> 16) / 32767.0;
            //    element.sinAlpha = (a >> 16) / 32767.0; // just guessing
            //    log.LogLine("cos alpha : " + element.cosAlpha);
            //    log.LogLine("sin alpha : " + element.sinAlpha);
            //    log.LogLine("alpha(cos, sin): " + Math.Acos(element.cosAlpha) * 180.0 / Math.PI + ", " + Math.Asin(element.sinAlpha) * 180.0 / Math.PI);
            //}
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

        private WorldElement ReadWorldElementV1(EngineVersion engineVersion, WorldTexFile texFile, ILogger log, byte[] data, int startOffset, WorldData worldData, DataReader reader, int elementArrayStart, int texX0, int texY0, int elementIdx)
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
                element.model = decodeModel(engineVersion, vifLogger, data, startOffset + vifDataOffset + vifStartOffset, vifLen * 0x10 - vifStartOffset, texWidth, texHeight);

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
            

            return worldData;
        }

        private string makeString(List<int> list)
        {
            var s = "";
            foreach (var i in list)
            {
                if (s.Length != 0)
                {
                    s += ", ";
                }
                s += HexUtil.formatHex(i);
            }

            return s;
        }

        private int[,] readTextureChunkOffsets(EngineVersion engineVersion, byte[] data, int offset, int x1, int y1, int x2, int y2)
        {
            var chunkOffsets = new int[100, 100];
            int addr;
            for (var y = y1; y <= y2; ++y)
            {
                for (var x = x1; x <= x2; ++x)
                {
                    var cellOffset = ((y - y1) * 100 + x - x1) * 8;
                    // This test is needed to deal with town.world in BGDA which addresses textures outside of the maximum x range.
                    if (data.Length >= offset + cellOffset + 4)
                    {
                        if (EngineVersion.ReturnToArms == engineVersion || EngineVersion.JusticeLeagueHeroes == engineVersion)
                        {
                            // TODO: Figure out what this should really be
                            addr = 0x800;
                        }
                        else
                        {
                            addr = DataUtil.getLEInt(data, offset + cellOffset);
                        }
                        chunkOffsets[y, x] = addr;
                    }
                }
            }
            return chunkOffsets;
        }

        private Dictionary<int, Model> modelMap = new Dictionary<int, Model>();

        public Model decodeModel(EngineVersion engineVersion, ILogger log, byte[] data, int startOffset, int length, int texWidth, int texHeight)
        {
            if (!modelMap.TryGetValue(startOffset, out var model))
            {
                model = new Model
                {
                    meshList = new List<Mesh>(1)
                };
                model.meshList.Add(VifDecoder.DecodeMesh(log, data, startOffset, length, texWidth, texHeight));
                modelMap.Add(startOffset, model);
            }
            return model;
        }
    }


}

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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Media3D;
using WorldExplorer.DataModel;
using WorldExplorer.Logging;

namespace WorldExplorer.DataLoaders
{
    public class WorldFileDecoder
    {
        public WorldData Decode(EngineVersion engineVersion, WorldTexFile texFile, ILogger log, byte[] data, int startOffset, int length)
        {
            WorldData worldData = new WorldData();

            var reader = new DataReader(data, startOffset, length);

            int numElements = reader.ReadInt32();

            reader.Skip(12); // Skipping 3 ints
            int numCols = reader.ReadInt32();
            int numRows = reader.ReadInt32();

            reader.Skip(12); // Skipping 3 ints
            int elementArrayStart = reader.ReadInt32();

            reader.Skip(8); // Skipping 2 ints
            int off38Cols = reader.ReadInt32();
            int off38Rows = reader.ReadInt32();
            int off38 = reader.ReadInt32();

            reader.Skip(28);
            int texll = reader.ReadInt32();
            int texur = reader.ReadInt32();
            int texX0 = texll % 100;
            int texY0 = texll / 100;
            int texX1 = texur % 100;
            int texY1 = texur / 100;

            reader.Skip(4);
            int worldTexOffsetsOffset = reader.ReadInt32();
            worldData.textureChunkOffsets = readTextureChunkOffsets(data, startOffset + worldTexOffsetsOffset, texX0, texY0, texX1, texY1);
            worldData.worldElements = new List<WorldElement>(numElements);

            for (int elementIdx = 0; elementIdx < numElements; ++elementIdx)
            {
                var element = new WorldElement();

                if (EngineVersion.ReturnToArms == engineVersion)
                {
                    reader.SetOffset(elementArrayStart + elementIdx * 0x3C);
                }
                else // Default to Dark Allience version
                {
                    reader.SetOffset(elementArrayStart + elementIdx * 0x38);
                }

                int vifDataOffset = reader.ReadInt32();

                if (EngineVersion.DarkAlliance == engineVersion)
                {
                    reader.Skip(4);
                }

                int vifLen = reader.ReadInt32();
                log.LogLine("-----------");
                log.LogLine("vifdata: " + vifDataOffset + ", " + vifLen);

                float x1 = reader.ReadFloat();
                float y1 = reader.ReadFloat();
                float z1 = reader.ReadFloat();
                float x2 = reader.ReadFloat();
                float y2 = reader.ReadFloat();
                float z2 = reader.ReadFloat();

                element.boundingBox = new Rect3D(x1, y1, z1, x2-x1, y2-y1, z2-z1);

                log.LogLine("Bounding Box: " + element.boundingBox.ToString());

                int textureNum = reader.ReadInt32() / 0x40;
                log.LogLine("Texture Num: " + textureNum);

                int texCellxy = reader.ReadInt16();
                int y = texCellxy / 100;
                int x = texCellxy % 100;

                if (textureNum != 0)
                {
                    element.Texture = texFile.GetBitmap(worldData.textureChunkOffsets[y, x], textureNum);
                }

                if (element.Texture != null)
                {
                    log.LogLine("Found in texture chunk: " + x + ", " + y);
                }

                var vifLogger = new StringLogger();
                int texWidth = 100;
                int texHeight = 100;
                if (element.Texture != null)
                {
                    texWidth = element.Texture.PixelWidth;
                    texHeight = element.Texture.PixelHeight;
                }

                byte nregs = data[startOffset + vifDataOffset + 0x10];
                int vifStartOffset = (nregs + 2) * 0x10;
                element.model = decodeModel(engineVersion, vifLogger, data, startOffset + vifDataOffset + vifStartOffset, vifLen * 0x10 - vifStartOffset, texWidth, texHeight);

                int tb = reader.ReadInt16();
                int tc = reader.ReadInt16();
                int td = reader.ReadInt16();
               
                log.LogLine("        : " + tb + ", " + tc + ", " + td);

                element.pos = new Vector3D(tb / 16.0, tc / 16.0, td / 16.0);

                int flags = reader.ReadInt32();
                if ((flags & 0x01) == 0)
                {
                    log.LogLine("Flags   : " + HexUtil.formatHexUShort(flags & 0xFFFF));
                    element.cosAlpha = reader.ReadInt16() / 32767.0;
                    element.sinAlpha = reader.ReadInt16() / 32767.0;
                    log.LogLine("cos alpha : " + element.cosAlpha);
                    log.LogLine("sin alpha : " + element.sinAlpha);
                    log.LogLine("alpha(cos, sin): " + Math.Acos(element.cosAlpha) * 180.0 / Math.PI + ", " + Math.Asin(element.sinAlpha) * 180.0 / Math.PI);

                    element.usesRotFlags = false;
                }
                else
                {
                    log.LogLine("Flags   : " + HexUtil.formatHex(flags));
                    element.xyzRotFlags = (flags >> 16) & 7;
                    element.usesRotFlags = true;
                    log.LogLine("Rot Flags   : " + element.xyzRotFlags);
                }

                element.negYaxis = (flags & 0x40) == 0x40;


                worldData.worldElements.Add(element);
            }

            return worldData;
        }

        private String makeString(List<int> list)
        {
            String s="";
            foreach (int i in list)
            {
                if (s.Length != 0)
                {
                    s += ", ";
                }
                s += HexUtil.formatHex(i);
            }

            return s;
        }

        private int[,] readTextureChunkOffsets(byte[] data, int offset, int x1, int y1, int x2, int y2)
        {
            int[,] chunkOffsets = new int[100, 100];
            int addr;
            for (int y = y1; y <= y2; ++y) {
                for (int x = x1; x <= x2; ++x) {
                    int cellOffset = ((y - y1) * 100 + x - x1) * 8;
                    addr = DataUtil.getLEInt(data, offset + cellOffset);
                    chunkOffsets[y, x] = addr;
                }
            }
            return chunkOffsets;
        }

        private Dictionary<int, Model> modelMap = new Dictionary<int, Model>();

        public Model decodeModel(EngineVersion engineVersion, ILogger log, byte[] data, int startOffset, int length, int texWidth, int texHeight)
        {
            Model model = null;
            if (!modelMap.TryGetValue(startOffset, out model)){
                model = new Model();
                model.meshList = new List<Mesh>(1);
                model.meshList.Add(VifDecoder.DecodeMesh(log, data, startOffset, length, texWidth, texHeight));
                modelMap.Add(startOffset, model);
            }
            return model;
        }
    }


}

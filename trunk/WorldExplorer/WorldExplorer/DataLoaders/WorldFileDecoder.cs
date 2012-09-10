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
        public WorldData Decode(WorldTexFile texFile, ILogger log, byte[] data, int startOffset, int length)
        {
            WorldData worldData = new WorldData();

            int numElements = DataUtil.getLEInt(data, startOffset + 0);

            int numCols = DataUtil.getLEInt(data, startOffset + 0x10);
            int numRows = DataUtil.getLEInt(data, startOffset + 0x14);

            int elementArrayStart = DataUtil.getLEInt(data, startOffset + 0x24);

            int off38Cols = DataUtil.getLEInt(data, startOffset + 0x30);
            int off38Rows = DataUtil.getLEInt(data, startOffset + 0x34);
            int off38 = DataUtil.getLEInt(data, startOffset + 0x38);

            int texll = DataUtil.getLEInt(data, startOffset + 0x58);
            int texur = DataUtil.getLEInt(data, startOffset + 0x5C);
            int texX0 = texll % 100;
            int texY0 = texll / 100;
            int texX1 = texur % 100;
            int texY1 = texur / 100;

            int worldTexOffsetsOffset = DataUtil.getLEInt(data, startOffset + 0x64);
            worldData.textureChunkOffsets = readTextureChunkOffsets(data, startOffset + worldTexOffsetsOffset, texX0, texY0, texX1, texY1);
            worldData.worldElements = new List<WorldElement>(numElements);
            for (int elementIdx = 0; elementIdx < numElements; ++elementIdx)
            {
                WorldElement element = new WorldElement();
                int elementStartOffset = startOffset + elementArrayStart + elementIdx * 0x38;

                int vifDataOffset = DataUtil.getLEInt(data, elementStartOffset);
                int vifLen = DataUtil.getLEInt(data, elementStartOffset+8);
                log.LogLine("-----------");
                log.LogLine("vifdata: " + vifDataOffset + ", " + vifLen);

                float x1 = DataUtil.getLEFloat(data, elementStartOffset + 0x0C);
                float y1 = DataUtil.getLEFloat(data, elementStartOffset + 0x10);
                float z1 = DataUtil.getLEFloat(data, elementStartOffset + 0x14);
                float x2 = DataUtil.getLEFloat(data, elementStartOffset + 0x18);
                float y2 = DataUtil.getLEFloat(data, elementStartOffset + 0x1C);
                float z2 = DataUtil.getLEFloat(data, elementStartOffset + 0x20);

                element.boundingBox = new Rect3D(x1, y1, z1, x2-x1, y2-y1, z2-z1);

                log.LogLine("Bounding Box: " + element.boundingBox.ToString());

                int textureNum = DataUtil.getLEInt(data, elementStartOffset + 0x24) / 0x40;
                log.LogLine("Texture Num: " + textureNum);

                int texCellxy = DataUtil.getLEShort(data, elementStartOffset + 0x28);
                int y = texCellxy / 100;
                int x = texCellxy % 100;
                element.Texture = texFile.GetBitmap(worldData.textureChunkOffsets[y, x], textureNum);
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
                element.model = decodeModel(vifLogger, data, startOffset + vifDataOffset + vifStartOffset, vifLen * 0x10 - vifStartOffset, texWidth, texHeight);

                int tb = DataUtil.getLEShort(data, elementStartOffset + 0x2A);
                int tc = DataUtil.getLEShort(data, elementStartOffset + 0x2C);
                int td = DataUtil.getLEShort(data, elementStartOffset + 0x2E);
               
                log.LogLine("        : " + tb + ", " + tc + ", " + td);

                element.pos = new Vector3D(tb / 16.0, tc / 16.0, td / 16.0);

                int flags = DataUtil.getLEInt(data, elementStartOffset + 0x30);
                if ((flags & 0x01) == 0)
                {
                    log.LogLine("Flags   : " + HexUtil.formatHexUShort(flags & 0xFFFF));
                    element.cosAlpha = DataUtil.getLEShort(data, elementStartOffset + 0x32) / 32767.0;
                    element.sinAlpha = DataUtil.getLEShort(data, elementStartOffset + 0x34) / 32767.0;
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

        public Model decodeModel(ILogger log, byte[] data, int startOffset, int length, int texWidth, int texHeight)
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

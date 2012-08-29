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

            int worldTexOffsetsOffset = DataUtil.getLEInt(data, startOffset + 0x64);
            worldData.textureChunkOffsets = readTextureChunkOffsets(data, startOffset + worldTexOffsetsOffset);
            log.LogLine("Texture chunk offsets: " + makeString(worldData.textureChunkOffsets));
            worldData.worldElements = new List<WorldElement>(numElements);
            for (int elementIdx = 0; elementIdx < numElements; ++elementIdx)
            {
                WorldElement element = new WorldElement();
                int elementStartOffset = startOffset + elementArrayStart + elementIdx * 0x38;

                int vifDataOffset = DataUtil.getLEInt(data, elementStartOffset);
                int vifLen = DataUtil.getLEInt(data, elementStartOffset+8);
                log.LogLine("-----------");
                log.LogLine("vifdata: " + vifDataOffset + ", " + vifLen);
                element.model = decodeModel(data, startOffset + vifDataOffset, vifLen);

                float x1 = DataUtil.getLEFloat(data, elementStartOffset + 0x0C);
                float y1 = DataUtil.getLEFloat(data, elementStartOffset + 0x10);
                float z1 = DataUtil.getLEFloat(data, elementStartOffset + 0x14);
                float x2 = DataUtil.getLEFloat(data, elementStartOffset + 0x18);
                float y2 = DataUtil.getLEFloat(data, elementStartOffset + 0x1C);
                float z2 = DataUtil.getLEFloat(data, elementStartOffset + 0x20);

                element.boundingBox = new Rect3D(x1, y1, z1, x2-x1, y2-y1, z2-z1);
                
                int textureChunk = DataUtil.getLEInt(data, elementStartOffset + 0x24);
                log.LogLine("Texture Chunk: " + textureChunk);

                element.Texture = texFile.GetBitmap(worldData.textureChunkOffsets[0] + textureChunk);

                int ta = DataUtil.getLEShort(data, elementStartOffset + 0x28);
                int tb = DataUtil.getLEShort(data, elementStartOffset + 0x2A);
                int tc = DataUtil.getLEShort(data, elementStartOffset + 0x2C);
                int td = DataUtil.getLEShort(data, elementStartOffset + 0x2E);
               
                log.LogLine("        : " + ta + ", " + tb + ", " + tc + ", " + td);

                int member30 = DataUtil.getLEUShort(data, elementStartOffset + 0x30);
                log.LogLine("        : " + HexUtil.formatHexUShort(member30));
                int member32 = DataUtil.getLEShort(data, elementStartOffset + 0x32);
                int member34 = DataUtil.getLEShort(data, elementStartOffset + 0x34);
                log.LogLine("        : " + member32 + ", " + member34);
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

        private List<int> readTextureChunkOffsets(byte[] data, int offset)
        {
            List<int> chunkOffsets = new List<int>();
            int addr;
            while ((addr = DataUtil.getLEInt(data, offset)) != 0)
            {
                chunkOffsets.Add(addr);
                offset += 8;

            }
            return chunkOffsets;
        }

        private Dictionary<int, Model> modelMap = new Dictionary<int, Model>();

        public Model decodeModel(byte[] data, int startOffset, int length)
        {
            Model model = null;
            if (!modelMap.TryGetValue(startOffset, out model)){

            }
            return model;
        }
    }


}

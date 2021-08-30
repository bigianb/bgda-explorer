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
using WorldExplorer.DataModel;
using WorldExplorer.Logging;

namespace WorldExplorer.DataLoaders
{
    public abstract class WorldFileDecoder
    {
        private Dictionary<int, Model> _modelCache = new Dictionary<int, Model>();

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
                var element = ReadWorldElement(engineVersion, texFile, log, data, startOffset, worldData, reader, elementArrayStart, texX0, texY0, elementIdx);
                if (element != null)
                    worldData.worldElements.Add(element);
            }

            return worldData;
        }

        protected abstract WorldElement ReadWorldElement(EngineVersion engineVersion, WorldTexFile texFile, ILogger log, byte[] data, int startOffset, WorldData worldData, DataReader reader, int elementArrayStart, int texX0, int texY0, int elementIdx);

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

        protected Model DecodeModel(EngineVersion engineVersion, ILogger log, byte[] data, int startOffset, int length, int texWidth, int texHeight)
        {
            if (!_modelCache.TryGetValue(startOffset, out var model))
            {
                model = new Model
                {
                    meshList = new List<Mesh>(1)
                };
                model.meshList.Add(VifDecoder.DecodeMesh(
                    log, 
                    data.AsSpan().Slice(startOffset, length),
                    texWidth, 
                    texHeight));
                _modelCache.Add(startOffset, model);
            }
            return model;
        }
    }


}

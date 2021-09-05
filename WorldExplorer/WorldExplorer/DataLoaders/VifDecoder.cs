/*  Copyright (C) 2012-2018 Ian Brown

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
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WorldExplorer.DataModel;
using WorldExplorer.Logging;

namespace WorldExplorer.DataLoaders
{
    public class VifDecoder
    {
        private const int NOP_CMD = 0;
        private const int STCYCL_CMD = 1;
        private const int ITOP_CMD = 4;
        private const int STMOD_CMD = 5;
        private const int FLUSH_CMD = 0x11;
        private const int MSCAL_CMD = 0x14;
        private const int STMASK_CMD = 0x20;
        private const int DIRECT_CMD = 0x50;

        public static List<Mesh> Decode(ILogger log, ReadOnlySpan<byte> data, int texturePixelWidth,
            int texturePixelHeight)
        {
            var sig = DataUtil.getLEInt(data, 0);
            var numMeshes = data[0x12] & 0xFF;
            var meshBlockOffset = 0x28;
            if (sig == 0x30332E31)
            {
                numMeshes = data[0x4A] & 0xFF;
                meshBlockOffset = 0x68;
            }

            if (0 == numMeshes)
            {
                numMeshes = 1;
                meshBlockOffset = 0x68;
            }

            // var offset1 = DataUtil.getLEInt(data, 0x24);
            List<Mesh> meshes = new();
            var totalNumChunks = 0;

            for (var meshNum = 0; meshNum < numMeshes; ++meshNum)
            {
                var offsetVerts = DataUtil.getLEInt(data, meshBlockOffset + (meshNum * 4));
                var offsetEndVerts = DataUtil.getLEInt(data, meshBlockOffset + 4 + (meshNum * 4));
                var chunks = ReadVerts(log, data.Slice(offsetVerts, offsetEndVerts - offsetVerts));
                var mesh = ChunksToMesh(chunks, texturePixelWidth, texturePixelHeight);
                meshes.Add(mesh);
                totalNumChunks += chunks.Count;
            }

            log.LogLine("Num Meshes=" + numMeshes);
            log.LogLine("Total Num Chunks=" + totalNumChunks);

            return meshes;
        }

        public static List<Chunk> DecodeChunks(ILogger log, ReadOnlySpan<byte> data, int texturePixelWidth,
            int texturePixelHeight)
        {
            var numMeshes = data[0x12] & 0xFF;
            List<Chunk> chunks = new();
            for (var meshNum = 0; meshNum < numMeshes; ++meshNum)
            {
                var offsetVerts = DataUtil.getLEInt(data, 0x28 + (meshNum * 4));
                var offsetEndVerts = DataUtil.getLEInt(data, 0x2C + (meshNum * 4));
                chunks.AddRange(ReadVerts(log, data.Slice(offsetVerts, offsetEndVerts - offsetVerts)));
            }

            return chunks;
        }

        public static Mesh DecodeMesh(ILogger log, ReadOnlySpan<byte> data, int texturePixelWidth,
            int texturePixelHeight)
        {
            var chunks = ReadVerts(log, data);
            var mesh = ChunksToMesh(chunks, texturePixelWidth, texturePixelHeight);

            return mesh;
        }

        // Finds which vertex weight object to use for the given vertex.
        private static VertexWeight FindVertexWeight(List<VertexWeight> weights, int vertexNum)
        {
            foreach (var weight in weights)
            {
                if (vertexNum >= weight.startVertex && vertexNum <= weight.endVertex)
                {
                    return weight;
                }
            }

            if (weights.Count != 0)
            {
                Debug.Fail("Failed to find vertex weight");
            }

            return new VertexWeight();
        }

        // Tile an S,T coord to 0 -> 1.
        private static Point TileST(Point pointIn)
        {
            var pointOut = pointIn;
            if (pointOut.X > 1.0)
            {
                pointOut.X = pointOut.X % 1;
            }

            if (pointOut.Y > 1.0)
            {
                pointOut.Y = pointOut.Y % 1;
            }

            return pointOut;
        }

        public static Mesh ChunksToMesh(List<Chunk> chunks, int texturePixelWidth, int texturePixelHeight)
        {
            var numVertices = 0;
            foreach (var chunk in chunks)
            {
                numVertices += chunk.Vertices.Count;
            }

            var triangleIndices = new Int32Collection();
            var positions = new Point3DCollection(numVertices);
            var normals = new Vector3DCollection(numVertices);
            var vertexWeights = new List<VertexWeight>();
            var uvCoords = new Point[numVertices];
            Point unInitPoint = new(-10000, -10000);
            for (var uv = 0; uv < uvCoords.Length; ++uv)
            {
                uvCoords[uv] = unInitPoint;
            }

            var vStart = 0;
            foreach (var chunk in chunks)
            {
                if (null == chunk.gifTag0)
                    // Hack to deal with JLH models. TODO: Fix this properly
                {
                    continue;
                }

                if ((chunk.gifTag0.prim & 0x07) != 4)
                {
                    Debug.Fail("Can only deal with tri strips");
                }

                foreach (var vertex in chunk.Vertices)
                {
                    Point3D point = new(vertex.X / 16.0, vertex.Y / 16.0, vertex.Z / 16.0);
                    positions.Add(point);
                }

                foreach (var normal in chunk.Normals)
                {
                    normals.Add(new Vector3D(normal.X / 127.0, normal.Y / 127.0, normal.Z / 127.0));
                }

                foreach (var vw in chunk.VertexWeights)
                {
                    var vwAdjusted = vw;
                    vwAdjusted.startVertex += vStart;
                    if (vwAdjusted.endVertex >= chunk.Vertices.Count)
                    {
                        vwAdjusted.endVertex = chunk.Vertices.Count - 1;
                    }

                    vwAdjusted.endVertex += vStart;
                    if (vw.startVertex <= chunk.Vertices.Count - 1)
                    {
                        vertexWeights.Add(vwAdjusted);
                    }
                }

                var vstrip = new int[chunk.gifTag0.nloop];
                var regsPerVertex = chunk.gifTag0.nreg;
                var numVlocs = chunk.VLocs.Count;
                var numVertsInChunk = chunk.Vertices.Count;
                for (var vlocIndx = 2; vlocIndx < numVlocs; ++vlocIndx)
                {
                    var v = vlocIndx - 2;
                    var stripIdx2 = (chunk.VLocs[vlocIndx].Value2 & 0x1FF) / regsPerVertex;
                    var stripIdx3 = (chunk.VLocs[vlocIndx].Value3 & 0x1FF) / regsPerVertex;
                    if (stripIdx3 < vstrip.Length && stripIdx2 < vstrip.Length)
                    {
                        vstrip[stripIdx3] = vstrip[stripIdx2] & 0x1FF;

                        var skip2 = (chunk.VLocs[vlocIndx].Value3 & 0x8000) == 0x8000;
                        if (skip2)
                        {
                            vstrip[stripIdx3] |= 0x8000;
                        }
                    }

                    var stripIdx = (chunk.VLocs[vlocIndx].Value1 & 0x1FF) / regsPerVertex;
                    var skip = (chunk.VLocs[vlocIndx].Value1 & 0x8000) == 0x8000;

                    if (v < numVertsInChunk && stripIdx < vstrip.Length)
                    {
                        vstrip[stripIdx] = skip ? v | 0x8000 : v;
                    }
                }

                int numExtraVlocs = chunk.extraVlocs[0];
                for (var extraVloc = 0; extraVloc < numExtraVlocs; ++extraVloc)
                {
                    var idx = (extraVloc * 4) + 4;
                    var stripIndxSrc = (chunk.extraVlocs[idx] & 0x1FF) / regsPerVertex;
                    var stripIndxDest = (chunk.extraVlocs[idx + 1] & 0x1FF) / regsPerVertex;
                    
                    vstrip[stripIndxDest] = (chunk.extraVlocs[idx + 1] & 0x8000) | (vstrip[stripIndxSrc] & 0x1FF);

                    stripIndxSrc = (chunk.extraVlocs[idx + 2] & 0x1FF) / regsPerVertex;
                    stripIndxDest = (chunk.extraVlocs[idx + 3] & 0x1FF) / regsPerVertex;
                    
                    vstrip[stripIndxDest] = (chunk.extraVlocs[idx + 3] & 0x8000) | (vstrip[stripIndxSrc] & 0x1FF);
                }

                var triIdx = 0;
                for (var i = 2; i < vstrip.Length; ++i)
                {
                    var vidx1 = vStart + (vstrip[i - 2] & 0xFF);
                    var vidx2 = vStart + (vstrip[i - 1] & 0xFF);
                    var vidx3 = vStart + (vstrip[i] & 0xFF);

                    var uv1 = i - 2;
                    var uv2 = i - 1;
                    var uv3 = i;

                    // Flip the faces (indices 1 and 2) to keep the winding rule consistent.
                    if ((triIdx & 1) == 1)
                    {
                        var temp = uv1;
                        uv1 = uv2;
                        uv2 = temp;

                        temp = vidx1;
                        vidx1 = vidx2;
                        vidx2 = temp;
                    }

                    if ((vstrip[i] & 0x8000) == 0)
                    {
                        // WPF really has S,T coords rather than u,v
                        var udiv = texturePixelWidth * 16.0;
                        var vdiv = texturePixelHeight * 16.0;

                        Point p1 = new(chunk.UVs[uv1].U / udiv, chunk.UVs[uv1].V / vdiv);
                        Point p2 = new(chunk.UVs[uv2].U / udiv, chunk.UVs[uv2].V / vdiv);
                        Point p3 = new(chunk.UVs[uv3].U / udiv, chunk.UVs[uv3].V / vdiv);

                        p1 = TileST(p1);
                        p2 = TileST(p2);
                        p3 = TileST(p3);

                        if (!unInitPoint.Equals(uvCoords[vidx1]) && !p1.Equals(uvCoords[vidx1]))
                        {
                            // There is more than 1 uv assigment to this vertex, so we need to duplicate it.
                            var originalVIdx = vidx1;
                            vidx1 = vStart + numVertsInChunk;
                            numVertsInChunk++;
                            positions.Add(positions.ElementAt(originalVIdx));
                            normals.Add(normals.ElementAt(originalVIdx));
                            Array.Resize(ref uvCoords, uvCoords.Length + 1);
                            uvCoords[uvCoords.Length - 1] = unInitPoint;
                            var weight = FindVertexWeight(chunk.VertexWeights, originalVIdx - vStart);
                            if (weight.boneWeight1 > 0)
                            {
                                var vw = weight;
                                vw.startVertex = vidx1;
                                vw.endVertex = vidx1;
                                vertexWeights.Add(vw);
                            }
                        }

                        if (!unInitPoint.Equals(uvCoords[vidx2]) && !p2.Equals(uvCoords[vidx2]))
                        {
                            // There is more than 1 uv assigment to this vertex, so we need to duplicate it.
                            var originalVIdx = vidx2;
                            vidx2 = vStart + numVertsInChunk;
                            numVertsInChunk++;
                            positions.Add(positions.ElementAt(originalVIdx));
                            normals.Add(normals.ElementAt(originalVIdx));
                            Array.Resize(ref uvCoords, uvCoords.Length + 1);
                            uvCoords[uvCoords.Length - 1] = unInitPoint;
                            var weight = FindVertexWeight(chunk.VertexWeights, originalVIdx - vStart);
                            if (weight.boneWeight1 > 0)
                            {
                                var vw = weight;
                                vw.startVertex = vidx2;
                                vw.endVertex = vidx2;
                                vertexWeights.Add(vw);
                            }
                        }

                        if (!unInitPoint.Equals(uvCoords[vidx3]) && !p3.Equals(uvCoords[vidx3]))
                        {
                            // There is more than 1 uv assigment to this vertex, so we need to duplicate it.
                            var originalVIdx = vidx3;
                            vidx3 = vStart + numVertsInChunk;
                            numVertsInChunk++;
                            positions.Add(positions.ElementAt(originalVIdx));
                            normals.Add(normals.ElementAt(originalVIdx));
                            Array.Resize(ref uvCoords, uvCoords.Length + 1);
                            uvCoords[uvCoords.Length - 1] = unInitPoint;
                            var weight = FindVertexWeight(chunk.VertexWeights, originalVIdx - vStart);
                            if (weight.boneWeight1 > 0)
                            {
                                var vw = weight;
                                vw.startVertex = vidx3;
                                vw.endVertex = vidx3;
                                vertexWeights.Add(vw);
                            }
                        }

                        uvCoords[vidx1] = p1;
                        uvCoords[vidx2] = p2;
                        uvCoords[vidx3] = p3;

                        // Double sided hack. Should fix this with normals really
                        triangleIndices.Add(vidx1);
                        triangleIndices.Add(vidx2);
                        triangleIndices.Add(vidx3);

                        triangleIndices.Add(vidx2);
                        triangleIndices.Add(vidx1);
                        triangleIndices.Add(vidx3);
                    }

                    ++triIdx;
                }

                vStart += numVertsInChunk;
            }

            var textureCoordinates = new PointCollection(uvCoords);
            return new Mesh(normals, positions, textureCoordinates, triangleIndices, vertexWeights);
        }

        public static List<Chunk> ReadVerts(ILogger log, ReadOnlySpan<byte> vertData)
        {
            List<Chunk> chunks = new();
            Chunk currentChunk = new();
            Chunk? previousChunk = null;
            var offset = 0;
            while (offset < vertData.Length)
            {
                var vifCommand = vertData[offset + 3] & 0x7f;
                var numCommand = vertData[offset + 2] & 0xff;
                int immCommand = DataUtil.getLEShort(vertData, offset);
                switch (vifCommand)
                {
                    case NOP_CMD:
                        DebugWrite(HexUtil.formatHex(offset) + " ");
                        DebugWriteLine("NOP");
                        offset += 4;
                        break;
                    case STCYCL_CMD:
                        DebugWrite(HexUtil.formatHex(offset) + " ");
                        DebugWriteLine("STCYCL: WL: " + (immCommand >> 8) + " CL: " + (immCommand & 0xFF));
                        offset += 4;
                        break;
                    case ITOP_CMD:
                        DebugWrite(HexUtil.formatHex(offset) + " ");
                        DebugWriteLine("ITOP: " + immCommand);
                        offset += 4;
                        break;
                    case STMOD_CMD:
                        DebugWrite(HexUtil.formatHex(offset) + " ");
                        DebugWriteLine("STMOD: " + immCommand);
                        offset += 4;
                        break;
                    case MSCAL_CMD:
                        DebugWrite(HexUtil.formatHex(offset) + " ");
                        DebugWriteLine("MSCAL: " + immCommand);
                        if (immCommand != 66 && immCommand != 68 && immCommand != 70)
                        {
                            DebugWriteLine("**** Microcode " + immCommand + " not supported");
                        }

                        currentChunk.mscalID = immCommand;
                        chunks.Add(currentChunk);
                        previousChunk = currentChunk;
                        currentChunk = new Chunk();

                        offset += 4;
                        break;
                    case STMASK_CMD:
                        DebugWrite(HexUtil.formatHex(offset) + " ");
                        offset += 4;
                        var stmask = DataUtil.getLEInt(vertData, offset);
                        DebugWriteLine("STMASK: " + stmask);
                        offset += 4;
                        break;
                    case FLUSH_CMD:
                        DebugWrite(HexUtil.formatHex(offset) + " ");
                        DebugWriteLine("FLUSH");
                        offset += 4;
                        break;
                    case DIRECT_CMD:
                        DebugWrite(HexUtil.formatHex(offset) + " ");
                        DebugWriteLine("DIRECT, " + (immCommand * 16) + " bytes");

                        var tags = new GIFTag[immCommand];

                        for (var i = 0; i < immCommand; i++)
                        {
                            tags[i] = new GIFTag();
                            tags[i].Parse(vertData.Slice(offset + 4 + (i * 16)));
                        }

                        currentChunk.DirectGifTags.AddRange(tags);

                        offset += 4;
                        offset += immCommand * 16;
                        break;
                    default:
                        if ((vifCommand & 0x60) == 0x60)
                        {
                            // unpack command
                            var mask = (vifCommand & 0x10) == 0x10;
                            var vn = (vifCommand >> 2) & 3;
                            var vl = vifCommand & 3;
                            var addr = immCommand & 0x1ff;
                            var flag = (immCommand & 0x8000) == 0x8000;
                            var usn = (immCommand & 0x4000) == 0x4000;

                            DebugWrite(HexUtil.formatHex(offset) + " ");
                            var debugMsg = "UNPACK: vn: " + vn + ", vl: " + vl + ", Addr: " + addr + ", num: " +
                                           numCommand;

                            if (flag)
                            {
                                debugMsg += ", Flag";
                            }

                            if (usn)
                            {
                                debugMsg += ", Unsigned";
                            }

                            if (mask)
                            {
                                debugMsg += ", Mask";
                            }

                            DebugWriteLine(debugMsg);
                            offset += 4;
                            if (vn == 1 && vl == 1)
                            {
                                // v2-16
                                // I don't know why but the UVs come after the MSCAL instruction.
                                if (previousChunk != null)
                                {
                                    for (var uvnum = 0; uvnum < numCommand; ++uvnum)
                                    {
                                        var u = DataUtil.getLEShort(vertData, offset);
                                        var v = DataUtil.getLEShort(vertData, offset + 2);
                                        previousChunk.UVs.Add(new UV(u, v));
                                        offset += 4;
                                    }
                                }
                                else
                                {
                                    var numBytes = numCommand * 4;
                                    offset += numBytes;
                                }
                            }
                            else if (vn == 2 && vl == 1)
                            {
                                // v3-16
                                // each vertex is 128 bits, so num is the number of vertices
                                for (var vnum = 0; vnum < numCommand; ++vnum)
                                {
                                    if (!usn)
                                    {
                                        var x = DataUtil.getLEShort(vertData, offset);
                                        var y = DataUtil.getLEShort(vertData, offset + 2);
                                        var z = DataUtil.getLEShort(vertData, offset + 4);
                                        offset += 6;

                                        Vertex vertex = new() {X = x, Y = y, Z = z};
                                        currentChunk.Vertices.Add(vertex);
                                    }
                                    else
                                    {
                                        int x = DataUtil.getLEUShort(vertData, offset);
                                        int y = DataUtil.getLEUShort(vertData, offset + 2);
                                        int z = DataUtil.getLEUShort(vertData, offset + 4);
                                        offset += 6;
                                        
                                        currentChunk.VLocs.Add(new(x, y, z));
                                    }
                                }

                                offset = (offset + 3) & ~3;
                            }
                            else if (vn == 2 && vl == 2)
                            {
                                // v3-8
                                var idx = offset;
                                for (var vnum = 0; vnum < numCommand; ++vnum)
                                {
                                    SByteVector vec = new()
                                    {
                                        X = (sbyte)vertData[idx++],
                                        Y = (sbyte)vertData[idx++],
                                        Z = (sbyte)vertData[idx++]
                                    };
                                    currentChunk.Normals.Add(vec);
                                }

                                var numBytes = ((numCommand * 3) + 3) & ~3;
                                offset += numBytes;
                            }
                            else if (vn == 3 && vl == 0)
                            {
                                // v4-32
                                log.LogLine("v4-32 data, " + numCommand + (numCommand == 1 ? " entry" : " entries") +
                                            ", addr=" + addr);
                                if (1 == numCommand)
                                {
                                    currentChunk.gifTag0 = new GIFTag();
                                    currentChunk.gifTag0.Parse(vertData.Slice(offset));
                                    DebugWrite(HexUtil.formatHex(offset) + " ");
                                    DebugWriteLine("GifTag: " + currentChunk.gifTag0);
                                }
                                else if (2 == numCommand)
                                {
                                    currentChunk.gifTag0 = new GIFTag();
                                    currentChunk.gifTag0.Parse(vertData.Slice(offset));
                                    currentChunk.gifTag1 = new GIFTag();
                                    currentChunk.gifTag1.Parse(vertData.Slice(offset + 16));

                                    DebugWrite(HexUtil.formatHex(offset) + " ");
                                    DebugWriteLine("GifTag0: " + currentChunk.gifTag0);
                                    DebugWrite(HexUtil.formatHex(offset) + " ");
                                    DebugWriteLine("GifTag1: " + currentChunk.gifTag1);
                                }
                                else
                                {
                                    log.LogLine("unknown number of gif commands.");
                                }

                                var numBytes = numCommand * 16;
                                offset += numBytes;
                            }
                            else if (vn == 3 && vl == 1)
                            {
                                // v4-16
                                log.LogLine("v4-16 data, " + numCommand + (numCommand == 1 ? " entry" : " entries") +
                                            ", addr=" + addr);
                                var numShorts = numCommand * 4;
                                if (usn)
                                {
                                    currentChunk.extraVlocs = new ushort[numShorts];
                                    for (var i = 0; i < numCommand; ++i)
                                    {
                                        currentChunk.extraVlocs[i * 4] =
                                            DataUtil.getLEUShort(vertData, offset + (i * 8));
                                        currentChunk.extraVlocs[(i * 4) + 1] =
                                            DataUtil.getLEUShort(vertData, offset + (i * 8) + 2);
                                        currentChunk.extraVlocs[(i * 4) + 2] =
                                            DataUtil.getLEUShort(vertData, offset + (i * 8) + 4);
                                        currentChunk.extraVlocs[(i * 4) + 3] =
                                            DataUtil.getLEUShort(vertData, offset + (i * 8) + 6);
                                    }
                                }
                                else
                                {
                                    log.LogLine("Unsupported tag");
                                }

                                offset += numShorts * 2;
                            }
                            else if (vn == 3 && vl == 2)
                            {
                                // v4-8
                                // var numBytes = numCommand * 4;
                                currentChunk.VertexWeights = new List<VertexWeight>();
                                var curVertex = 0;
                                for (var i = 0; i < numCommand; ++i)
                                {
                                    VertexWeight vw = new()
                                    {
                                        startVertex = curVertex,
                                        bone1 = vertData[offset++] / 4,
                                        boneWeight1 = vertData[offset++],
                                        bone2 = vertData[offset++]
                                    };
                                    if (vw.bone2 == 0xFF)
                                    {
                                        // Single bone                                       
                                        vw.boneWeight2 = 0;
                                        int count = vertData[offset++];
                                        curVertex += count;
                                    }
                                    else
                                    {
                                        vw.bone2 /= 4;
                                        vw.boneWeight2 = vertData[offset++];
                                        ++curVertex;

                                        if (vw.boneWeight1 + vw.boneWeight2 < 255)
                                        {
                                            ++i;
                                            vw.bone3 = vertData[offset++] / 4;
                                            vw.boneWeight3 = vertData[offset++];
                                            vw.bone4 = vertData[offset++];
                                            int bw4 = vertData[offset++];
                                            if (vw.bone4 != 255)
                                            {
                                                vw.bone4 /= 4;
                                                vw.boneWeight4 = bw4;
                                            }
                                        }
                                    }

                                    vw.endVertex = curVertex - 1;
                                    currentChunk.VertexWeights.Add(vw);
                                }
                            }
                            else
                            {
                                DebugWriteLine("Unknown vnvl combination: vn=" + vn + ", vl=" + vl);
                                offset = vertData.Length;
                            }
                        }
                        else
                        {
                            DebugWriteLine("Unknown command: " + vifCommand);
                            offset = vertData.Length;
                        }

                        break;
                }
            }

            return chunks;
        }

        private static void DebugWrite(string msg)
        {
            //Debug.Write(msg);
        }

        private static void DebugWriteLine(string msg)
        {
            //Debug.WriteLine(msg);
        }

        public class Vertex
        {
            public short X;
            public short Y;
            public short Z;
        }

        public class SByteVector
        {
            public sbyte X;
            public sbyte Y;
            public sbyte Z;
        }

        public class VLoc
        {
            public readonly int Value1;
            public readonly int Value2;
            public readonly int Value3;

            public VLoc(int v1, int v2, int v3)
            {
                this.Value1 = v1;
                this.Value2 = v2;
                this.Value3 = v3;
            }

            public override string ToString()
            {
                return HexUtil.formatHexUShort(Value1) + ", " + HexUtil.formatHexUShort(Value2) + ", " +
                       HexUtil.formatHexUShort(Value3);
            }
        }

        public class UV
        {
            public readonly short U;
            public readonly short V;

            public UV(short u, short v)
            {
                this.U = u;
                this.V = v;
            }
        }

        public class Chunk
        {
            public readonly List<GIFTag> DirectGifTags = new();
            public ushort[] extraVlocs;
            public GIFTag gifTag0;
            public GIFTag gifTag1;
            public int mscalID;
            public readonly List<SByteVector> Normals = new();
            public readonly List<UV> UVs = new();
            public List<VertexWeight> VertexWeights = new();
            public readonly List<Vertex> Vertices = new();
            public readonly List<VLoc> VLocs = new();
        }
    }
}
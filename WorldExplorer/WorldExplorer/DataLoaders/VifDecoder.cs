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
using System.Linq;
using System.Windows.Media.Media3D;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows;
using WorldExplorer.Logging;
using WorldExplorer.DataModel;

namespace WorldExplorer.DataLoaders
{
    public class VifDecoder
    {
        public static List<Mesh> Decode(ILogger log, byte[] data, int startOffset, int length, int texturePixelWidth, int texturePixelHeight)
        {
            int numMeshes = data[startOffset + 0x12] & 0xFF;
            int meshBlockOffset = 0x28;
            if (0 == numMeshes)
            {
                numMeshes = 1;
                meshBlockOffset = 0x68;
            }
            int offset1 = DataUtil.getLEInt(data, startOffset + 0x24);
            List<Mesh> meshes = new List<Mesh>();
            int totalNumChunks = 0;
            for (int meshNum = 0; meshNum < numMeshes; ++meshNum) {
                int offsetVerts = DataUtil.getLEInt(data, startOffset + meshBlockOffset + meshNum * 4);
                int offsetEndVerts = DataUtil.getLEInt(data, startOffset + meshBlockOffset + 4 + meshNum * 4);
                var chunks = ReadVerts(log, data, startOffset + offsetVerts, startOffset + offsetEndVerts);
                var Mesh = ChunksToMesh(log, chunks, texturePixelWidth, texturePixelHeight);
                meshes.Add(Mesh);
                totalNumChunks += chunks.Count;
            }
            log.LogLine("Num Meshes="+numMeshes);
            log.LogLine("Total Num Chunks=" + totalNumChunks);

            return meshes;
        }

        public static List<Chunk> DecodeChunks(ILogger log, byte[] data, int startOffset, int length, int texturePixelWidth, int texturePixelHeight)
        {
            int numMeshes = data[startOffset + 0x12] & 0xFF;
            List<Chunk> chunks = new List<Chunk>();
            for (int meshNum = 0; meshNum < numMeshes; ++meshNum)
            {
                int offsetVerts = DataUtil.getLEInt(data, startOffset + 0x28 + meshNum * 4);
                int offsetEndVerts = DataUtil.getLEInt(data, startOffset + 0x2C + meshNum * 4);
                chunks.AddRange(ReadVerts(log, data, startOffset + offsetVerts, startOffset + offsetEndVerts));
            }
            return chunks;
        }

        public static Mesh DecodeMesh(ILogger log, byte[] data, int startOffset, int length, int texturePixelWidth, int texturePixelHeight)
        {           
            var chunks = ReadVerts(log, data, startOffset, startOffset + length);
            var mesh = ChunksToMesh(log, chunks, texturePixelWidth, texturePixelHeight);

            return mesh;
        }

        // Finds which vertex weight object to use for the given vertex.
        private static VertexWeight FindVertexWeight(List<VertexWeight> weights, int vertexNum)
        {

            foreach (var weight in weights) {
                if (vertexNum >= weight.startVertex && vertexNum <= weight.endVertex) {
                    return weight;
                }
            }
            if (weights.Count != 0) {
                Debug.Fail("Failed to find vertex weight");
            }
            return new VertexWeight();
        }

        // Tile an S,T coord to 0 -> 1.
        private static Point TileST(Point pointIn)
        {
            Point pointOut=pointIn;
            if (pointOut.X > 1.0)
            {
                pointOut.X = pointOut.X%1;
            }
            if (pointOut.Y > 1.0)
            {
                pointOut.Y = pointOut.Y%1;
            }
            return pointOut;
        }

        public static Mesh ChunksToMesh(ILogger log, List<Chunk> chunks, int texturePixelWidth, int texturePixelHeight)
        {
            Mesh mesh = new Mesh();
            int numVertices = 0;
            foreach (Chunk chunk in chunks) {
                numVertices += chunk.vertices.Count;
            }
            mesh.TriangleIndices = new Int32Collection();
            mesh.Positions = new Point3DCollection(numVertices);
            mesh.Normals = new Vector3DCollection(numVertices);
            mesh.vertexWeights = new List<VertexWeight>();
            var uvCoords = new Point[numVertices];
            Point uninitPoint = new Point(-10000, -10000);
            for (int uv = 0; uv < uvCoords.Length; ++uv)
            {
                uvCoords[uv] = uninitPoint;
            }
            int vstart = 0;
            foreach (var chunk in chunks) {
                if ((chunk.gifTag0.prim & 0x07) != 4) {
                    Debug.Fail("Can only deal with tri strips");
                }

                foreach (var vertex in chunk.vertices) {
                    var point = new Point3D(vertex.x / 16.0, vertex.y / 16.0, vertex.z / 16.0);
                    mesh.Positions.Add(point);
                }
                foreach (var normal in chunk.normals) {
                    mesh.Normals.Add(new Vector3D(normal.x / 127.0, normal.y / 127.0, normal.z / 127.0));
                }
                foreach (VertexWeight vw in chunk.vertexWeights) {
                    VertexWeight vwAdjusted = vw;
                    vwAdjusted.startVertex += vstart;
                    if (vwAdjusted.endVertex >= chunk.vertices.Count) {
                        vwAdjusted.endVertex = chunk.vertices.Count - 1;
                    }
                    vwAdjusted.endVertex += vstart;
                    if (vw.startVertex <= (chunk.vertices.Count-1)) {
                        mesh.vertexWeights.Add(vwAdjusted);
                    }
                }
                int[] vstrip = new int[chunk.gifTag0.nloop];
                int regsPerVertex = chunk.gifTag0.nreg;
                int numVlocs = chunk.vlocs.Count;
                int numVertsInChunk = chunk.vertices.Count;
                for (int vlocIndx = 2; vlocIndx < numVlocs; ++vlocIndx) {
                    int v = vlocIndx - 2;
                    int stripIdx2 = (chunk.vlocs[vlocIndx].v2 & 0x1FF) / regsPerVertex;
                    int stripIdx3 = (chunk.vlocs[vlocIndx].v3 & 0x1FF) / regsPerVertex;
                    if (stripIdx3 < vstrip.Length && stripIdx2 < vstrip.Length) {
                        vstrip[stripIdx3] = vstrip[stripIdx2] & 0x1FF;

                        bool skip2 = (chunk.vlocs[vlocIndx].v3 & 0x8000) == 0x8000;
                        if (skip2) {
                            vstrip[stripIdx3] |= 0x8000;
                        }
                    }
                    int stripIdx = (chunk.vlocs[vlocIndx].v1 & 0x1FF) / regsPerVertex;
                    bool skip = (chunk.vlocs[vlocIndx].v1 & 0x8000) == 0x8000;

                    if (v < numVertsInChunk && stripIdx < vstrip.Length) {
                        vstrip[stripIdx] = skip ? (v | 0x8000) : v;
                    }
                }
                int numExtraVlocs = chunk.extraVlocs[0];
                for (int extraVloc = 0; extraVloc < numExtraVlocs; ++extraVloc) {
                    int idx = extraVloc * 4 + 4;
                    int stripIndxSrc = (chunk.extraVlocs[idx] & 0x1FF) / regsPerVertex;
                    int stripIndxDest = (chunk.extraVlocs[idx + 1] & 0x1FF) / regsPerVertex; ;
                    vstrip[stripIndxDest] = (chunk.extraVlocs[idx + 1] & 0x8000) | (vstrip[stripIndxSrc] & 0x1FF);

                    stripIndxSrc = (chunk.extraVlocs[idx + 2] & 0x1FF) / regsPerVertex;
                    stripIndxDest = (chunk.extraVlocs[idx + 3] & 0x1FF) / regsPerVertex; ;
                    vstrip[stripIndxDest] = (chunk.extraVlocs[idx + 3] & 0x8000) | (vstrip[stripIndxSrc] & 0x1FF);
                }
                int triIdx = 0;
                for (int i = 2; i < vstrip.Length; ++i) {
                    int vidx1 = vstart + (vstrip[i - 2] & 0xFF);
                    int vidx2 = vstart + (vstrip[i - 1] & 0xFF);
                    int vidx3 = vstart + (vstrip[i] & 0xFF);

                    int uv1 = i - 2;
                    int uv2 = i - 1;
                    int uv3 = i;

                    // Flip the faces (indices 1 and 2) to keep the winding rule consistent.
                    if ((triIdx & 1) == 1) {
                        int temp = uv1;
                        uv1 = uv2;
                        uv2 = temp;

                        temp = vidx1;
                        vidx1 = vidx2;
                        vidx2 = temp;
                    }

                    if ((vstrip[i] & 0x8000) == 0) {
                        // WPF really has S,T coords rather than u,v
                        double udiv = texturePixelWidth * 16.0;
                        double vdiv = texturePixelHeight * 16.0;

                        var p1 = new Point(chunk.uvs[uv1].u / udiv, chunk.uvs[uv1].v / vdiv);
                        var p2 = new Point(chunk.uvs[uv2].u / udiv, chunk.uvs[uv2].v / vdiv);
                        var p3 = new Point(chunk.uvs[uv3].u / udiv, chunk.uvs[uv3].v / vdiv);

                        p1 = TileST(p1);
                        p2 = TileST(p2);
                        p3 = TileST(p3);

                        if (!uninitPoint.Equals(uvCoords[vidx1]) && !p1.Equals(uvCoords[vidx1]))
                        {
                            // There is more than 1 uv assigment to this vertex, so we need to duplicate it.
                            int originalVIdx = vidx1;
                            vidx1 = vstart + numVertsInChunk;
                            numVertsInChunk++;
                            mesh.Positions.Add(mesh.Positions.ElementAt(originalVIdx));
                            mesh.Normals.Add(mesh.Normals.ElementAt(originalVIdx));
                            Array.Resize(ref uvCoords, uvCoords.Length+1);
                            uvCoords[uvCoords.Length - 1] = uninitPoint;
                            var weight = FindVertexWeight(chunk.vertexWeights, originalVIdx - vstart);
                            if (weight.boneWeight1 > 0)
                            {
                                var vw = weight;
                                vw.startVertex = vidx1;
                                vw.endVertex = vidx1;
                                mesh.vertexWeights.Add(vw);
                            }
                        }
                        if (!uninitPoint.Equals(uvCoords[vidx2]) && !p2.Equals(uvCoords[vidx2]))
                        {
                            // There is more than 1 uv assigment to this vertex, so we need to duplicate it.
                            int originalVIdx = vidx2;
                            vidx2 = vstart + numVertsInChunk;
                            numVertsInChunk++;
                            mesh.Positions.Add(mesh.Positions.ElementAt(originalVIdx));
                            mesh.Normals.Add(mesh.Normals.ElementAt(originalVIdx));
                            Array.Resize(ref uvCoords, uvCoords.Length + 1);
                            uvCoords[uvCoords.Length - 1] = uninitPoint;
                            var weight = FindVertexWeight(chunk.vertexWeights, originalVIdx - vstart);
                            if (weight.boneWeight1 > 0)
                            {
                                var vw = weight;
                                vw.startVertex = vidx2;
                                vw.endVertex = vidx2;
                                mesh.vertexWeights.Add(vw);
                            }
                        }
                        if (!uninitPoint.Equals(uvCoords[vidx3]) && !p3.Equals(uvCoords[vidx3]))
                        {
                            // There is more than 1 uv assigment to this vertex, so we need to duplicate it.
                            int originalVIdx = vidx3;
                            vidx3 = vstart + numVertsInChunk;
                            numVertsInChunk++;
                            mesh.Positions.Add(mesh.Positions.ElementAt(originalVIdx));
                            mesh.Normals.Add(mesh.Normals.ElementAt(originalVIdx));
                            Array.Resize(ref uvCoords, uvCoords.Length + 1);
                            uvCoords[uvCoords.Length - 1] = uninitPoint;
                            var weight = FindVertexWeight(chunk.vertexWeights, originalVIdx - vstart);
                            if (weight.boneWeight1 > 0)
                            {
                                var vw = weight;
                                vw.startVertex = vidx3;
                                vw.endVertex = vidx3;
                                mesh.vertexWeights.Add(vw);
                            }
                        }

                        uvCoords[vidx1] = p1;
                        uvCoords[vidx2] = p2;
                        uvCoords[vidx3] = p3;
                        
                        // Double sided hack. Should fix this with normals really
                        mesh.TriangleIndices.Add(vidx1);
                        mesh.TriangleIndices.Add(vidx2);
                        mesh.TriangleIndices.Add(vidx3);

                        mesh.TriangleIndices.Add(vidx2);
                        mesh.TriangleIndices.Add(vidx1);
                        mesh.TriangleIndices.Add(vidx3);
                    }
                    ++triIdx;
                }
                vstart += numVertsInChunk;
            }
            mesh.TextureCoordinates = new PointCollection(uvCoords);
            return mesh;
        }

        public class Vertex
        {
            public short x;
            public short y;
            public short z;
        }

        public class SByteVector
        {
            public sbyte x;
            public sbyte y;
            public sbyte z;
        }

        public class VLoc
        {
            public int v1;
            public int v2;
            public int v3;

            public override String ToString()
            {
                return HexUtil.formatHexUShort(v1) + ", " + HexUtil.formatHexUShort(v2) + ", " + HexUtil.formatHexUShort(v3);
            }
        }

        public class UV
        {
            public UV(short u, short v)
            {
                this.u = u;
                this.v = v;
            }

            public short u;
            public short v;
        }

        public class Chunk
        {
            public int mscalID = 0;
            public GIFTag gifTag0 = null;
            public GIFTag gifTag1 = null;
            public List<Vertex> vertices = new List<Vertex>();
            public List<SByteVector> normals = new List<SByteVector>();
            public List<VLoc> vlocs = new List<VLoc>();
            public List<UV> uvs = new List<UV>();
            public List<VertexWeight> vertexWeights = new List<VertexWeight>();
            public ushort[] extraVlocs;
            public List<GIFTag> DIRECTGifTags = new List<GIFTag>(); 
        }

        private const int NOP_CMD = 0;
        private const int STCYCL_CMD = 1;
        private const int ITOP_CMD = 4;
        private const int STMOD_CMD = 5;
        private const int FLUSH_CMD = 0x11;
        private const int MSCAL_CMD = 0x14;
        private const int STMASK_CMD = 0x20;
        private const int DIRECT_CMD = 0x50;

        public static List<Chunk> ReadVerts(ILogger log, byte[] fileData, int offset, int endOffset)
        {
            var chunks = new List<Chunk>();
            Chunk currentChunk = new Chunk();
            Chunk previousChunk = null;
            while (offset < endOffset) {
                int vifCommand = fileData[offset + 3] & 0x7f;
                int numCommand = fileData[offset + 2] & 0xff;
                int immCommand = DataUtil.getLEShort(fileData, offset);
                switch (vifCommand) {
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
                        int stmask = DataUtil.getLEInt(fileData, offset);
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
                        DebugWriteLine("DIRECT, " + immCommand*16 + " bytes");

                        GIFTag[] tags = new GIFTag[immCommand];

                        for (int i = 0; i < immCommand; i++)
                        {
                            tags[i] = new GIFTag();
                            tags[i].parse(fileData, offset + 4 + i*16);
                        }
                        currentChunk.DIRECTGifTags.AddRange(tags);

                        offset += 4;
                        offset += immCommand * 16;
                        break;
                    default:
                        if ((vifCommand & 0x60) == 0x60) {
                            // unpack command
                            bool mask = ((vifCommand & 0x10) == 0x10);
                            int vn = (vifCommand >> 2) & 3;
                            int vl = vifCommand & 3;
                            int addr = immCommand & 0x1ff;
                            bool flag = (immCommand & 0x8000) == 0x8000;
                            bool usn = (immCommand & 0x4000) == 0x4000;

                            DebugWrite(HexUtil.formatHex(offset) + " ");
                            String debugMsg = "UNPACK: vn: " + vn + ", vl: " + vl + ", Addr: " + addr + ", num: " + numCommand;

                            if (flag) {
                                debugMsg += ", Flag";
                            }
                            if (usn) {
                                debugMsg += ", Unsigned";
                            }
                            if (mask) {
                                debugMsg += ", Mask";
                            }
                            DebugWriteLine(debugMsg);
                            offset += 4;
                            if (vn == 1 && vl == 1) {
                                // v2-16
                                // I don't know why but the UVs come after the MSCAL instruction.
                                if (previousChunk != null) {
                                    for (int uvnum = 0; uvnum < numCommand; ++uvnum) {
                                        short u = DataUtil.getLEShort(fileData, offset);
                                        short v = DataUtil.getLEShort(fileData, offset + 2);
                                        previousChunk.uvs.Add(new UV(u, v));
                                        offset += 4;
                                    }
                                } else {
                                    int numBytes = numCommand * 4;
                                    offset += numBytes;
                                }
                            } else if (vn == 2 && vl == 1) {
                                // v3-16
                                // each vertex is 128 bits, so num is the number of vertices
                                for (int vnum = 0; vnum < numCommand; ++vnum) {
                                    if (!usn) {
                                        short x = DataUtil.getLEShort(fileData, offset);
                                        short y = DataUtil.getLEShort(fileData, offset + 2);
                                        short z = DataUtil.getLEShort(fileData, offset + 4);
                                        offset += 6;

                                        Vertex vertex = new Vertex();
                                        vertex.x = x;
                                        vertex.y = y;
                                        vertex.z = z;
                                        currentChunk.vertices.Add(vertex);
                                    } else {
                                        int x = DataUtil.getLEUShort(fileData, offset);
                                        int y = DataUtil.getLEUShort(fileData, offset + 2);
                                        int z = DataUtil.getLEUShort(fileData, offset + 4);
                                        offset += 6;

                                        VLoc vloc = new VLoc();
                                        vloc.v1 = x;
                                        vloc.v2 = y;
                                        vloc.v3 = z;
                                        currentChunk.vlocs.Add(vloc);
                                    }
                                }
                                offset = (offset + 3) & ~3;
                            } else if (vn == 2 && vl == 2) {
                                // v3-8
                                int idx = offset;
                                for (int vnum = 0; vnum < numCommand; ++vnum) {
                                    SByteVector vec = new SByteVector();
                                    vec.x = (sbyte)fileData[idx++];
                                    vec.y = (sbyte)fileData[idx++];
                                    vec.z = (sbyte)fileData[idx++];
                                    currentChunk.normals.Add(vec);
                                }
                                int numBytes = ((numCommand * 3) + 3) & ~3;
                                offset += numBytes;
                            } else if (vn == 3 && vl == 0) {
                                // v4-32
                                log.LogLine("v4-32 data, " + numCommand + (numCommand == 1 ? " entry" : " entries") + ", addr=" + addr);
                                if (1 == numCommand) {
                                    currentChunk.gifTag0 = new GIFTag();
                                    currentChunk.gifTag0.parse(fileData, offset);
                                    DebugWrite(HexUtil.formatHex(offset) + " ");
                                    DebugWriteLine("GifTag: " + currentChunk.gifTag0.ToString());
                                } else if (2 == numCommand) {
                                    currentChunk.gifTag0 = new GIFTag();
                                    currentChunk.gifTag0.parse(fileData, offset);
                                    currentChunk.gifTag1 = new GIFTag();
                                    currentChunk.gifTag1.parse(fileData, offset + 16);

                                    DebugWrite(HexUtil.formatHex(offset) + " ");
                                    DebugWriteLine("GifTag0: " + currentChunk.gifTag0.ToString());
                                    DebugWrite(HexUtil.formatHex(offset) + " ");
                                    DebugWriteLine("GifTag1: " + currentChunk.gifTag1.ToString());
                                } else {
                                    log.LogLine("unknown number of gif commands.");
                                }
                                int numBytes = numCommand * 16;
                                offset += numBytes;
                            } else if (vn == 3 && vl == 1) {
                                // v4-16
                                log.LogLine("v4-16 data, " + numCommand + (numCommand == 1 ? " entry" : " entries") + ", addr=" + addr);
                                int numShorts = numCommand * 4;
                                if (usn) {
                                    currentChunk.extraVlocs = new ushort[numShorts];
                                    for (int i = 0; i < numCommand; ++i) {
                                        currentChunk.extraVlocs[i*4] = DataUtil.getLEUShort(fileData, offset + i * 8);
                                        currentChunk.extraVlocs[i * 4 + 1] = DataUtil.getLEUShort(fileData, offset + i * 8 + 2);
                                        currentChunk.extraVlocs[i * 4 + 2] = DataUtil.getLEUShort(fileData, offset + i * 8 + 4);
                                        currentChunk.extraVlocs[i * 4 + 3] = DataUtil.getLEUShort(fileData, offset + i * 8 + 6);
                                    }
                                } else {
                                    log.LogLine("Unsupported tag");
                                }
                                offset += numShorts * 2;
                            } else if (vn == 3 && vl == 2) {
                                // v4-8
                                int numBytes = numCommand * 4;
                                currentChunk.vertexWeights = new List<VertexWeight>();
                                int curVertex=0;
                                for (int i = 0; i < numCommand; ++i) {
                                    VertexWeight vw = new VertexWeight();
                                    vw.startVertex = curVertex;
                                    vw.bone1 = fileData[offset++] / 4;
                                    vw.boneWeight1 = fileData[offset++];
                                    vw.bone2 = fileData[offset++];
                                    if (vw.bone2 == 0xFF) {
                                        // Single bone                                       
                                        vw.boneWeight2 = 0;
                                        int count = fileData[offset++];
                                        curVertex += count;
                                    } else {
                                        vw.bone2 /= 4;
                                        vw.boneWeight2 = fileData[offset++];
                                        ++curVertex;

                                        if (vw.boneWeight1 + vw.boneWeight2 < 255)
                                        {
                                            ++i;
                                            vw.bone3 = fileData[offset++] / 4;
                                            vw.boneWeight3 = fileData[offset++];
                                            vw.bone4 = fileData[offset++];
                                            int bw4 = fileData[offset++];
                                            if (vw.bone4 != 255)
                                            {
                                                vw.bone4 /= 4;
                                                vw.boneWeight4 = bw4;
                                            }
                                        }

                                    }
                                    vw.endVertex = curVertex - 1;
                                    currentChunk.vertexWeights.Add(vw);
                                }
                                
                            } else {
                                DebugWriteLine("Unknown vnvl combination: vn=" + vn + ", vl=" + vl);
                                offset = endOffset;
                            }
                        } else {
                            DebugWriteLine("Unknown command: " + vifCommand);
                            offset = endOffset;
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
    }
}

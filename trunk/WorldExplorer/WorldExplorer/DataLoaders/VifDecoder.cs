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
using System.Windows.Media.Media3D;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;
using WorldExplorer.Logging;

namespace WorldExplorer.DataLoaders
{
    class VifDecoder
    {
        public static Model3D Decode(ILogger log, byte[] data, int startOffset, int length, BitmapSource texture, AnimData pose, int frame)
        {
            int numMeshes = data[startOffset + 0x12] & 0xFF;
            int offset1 = DataUtil.getLEInt(data, startOffset + 0x24);
            List<List<Chunk>> meshes = new List<List<Chunk>>();
            int totalNumChunks = 0;
            for (int meshNum = 0; meshNum < numMeshes; ++meshNum) {
                int offsetVerts = DataUtil.getLEInt(data, startOffset + 0x28 + meshNum * 4);
                int offsetEndVerts = DataUtil.getLEInt(data, startOffset + 0x2C + meshNum * 4);
                var chunks = ReadVerts(log, data, startOffset + offsetVerts, startOffset + offsetEndVerts);
                meshes.Add(chunks);
                totalNumChunks += chunks.Count;
            }
            log.LogLine("Num Meshes="+numMeshes);
            log.LogLine("Total Num Chunks=" + totalNumChunks);
            int meshNo=1;
            foreach (var mesh in meshes)
            {
                log.LogLine("Mesh " + meshNo + ", num chunks=" + mesh.Count);
                ++meshNo;
            }
            return CreateModel3D(meshes, texture, pose, frame);
        }

        private static Model3D CreateModel3D(List<List<Chunk>> meshGroups, BitmapSource texture, AnimData pose, int frame)
        {
            GeometryModel3D model = new GeometryModel3D();
            var mesh = new MeshGeometry3D();

            int numVertices = 0;
            foreach (var meshGroup in meshGroups)
            {
                foreach (Chunk chunk in meshGroup)
                {
                    numVertices += chunk.vertices.Count;
                }
            }
            var triangleIndices = new Int32Collection();
            var positions = new Point3DCollection(numVertices);
            var normals = new Vector3DCollection(numVertices);
            var uvCoords = new Point[numVertices];
            int vstart = 0;
            int meshNo = 0;
            int chunkNo = 0;
            foreach (var meshGroup in meshGroups)
            {
                foreach (var chunk in meshGroup)
                {
                    if ((chunk.gifTag0.prim & 0x07) != 4)
                    {
                        Debug.Fail("Can only deal with tri strips");
                    }
                    AnimMeshPose thisPose = null;
                    if (pose != null)
                    {
                        int poseMesh = pose.NumBones > chunkNo ? chunkNo : pose.NumBones - 1;
                        thisPose = pose.perFramePoses[frame, poseMesh];
                    }
                    foreach (var vertex in chunk.vertices)
                    {
                        /**
                        if (thisPose != null)
                        {
                            double x = vertex.x / 127.0 + thisPose.Position.X;
                            double y = vertex.y / 127.0 + thisPose.Position.Y;
                            double z = vertex.z / 127.0 + thisPose.Position.Z;
                            positions.Add(new Point3D(x, y, z));
                        }
                        else
                        {
                         */
                            positions.Add(new Point3D(vertex.x / 127.0, vertex.y / 127.0, vertex.z / 127.0));
                        //}
                    }
                    foreach (var normal in chunk.normals)
                    {
                        normals.Add(new Vector3D(normal.x / 127.0, normal.y / 127.0, normal.z / 127.0));
                    }
                    int[] vstrip = new int[chunk.gifTag0.nloop];
                    int regsPerVertex = chunk.gifTag0.nreg;
                    int numVlocs = chunk.vlocs.Count;
                    int numVerts = chunk.vertices.Count;
                    for (int vlocIndx = 2; vlocIndx < numVlocs; ++vlocIndx)
                    {
                        int v = vlocIndx - 2;
                        int stripIdx2 = (chunk.vlocs[vlocIndx].v2 & 0x1FF) / regsPerVertex;
                        int stripIdx3 = (chunk.vlocs[vlocIndx].v3 & 0x1FF) / regsPerVertex;
                        if (stripIdx3 < vstrip.Length && stripIdx2 < vstrip.Length)
                        {
                            vstrip[stripIdx3] = vstrip[stripIdx2] & 0x1FF;

                            bool skip2 = (chunk.vlocs[vlocIndx].v3 & 0x8000) == 0x8000;
                            if (skip2)
                            {
                                vstrip[stripIdx3] |= 0x8000;
                            }
                        }
                        int stripIdx = (chunk.vlocs[vlocIndx].v1 & 0x1FF) / regsPerVertex;
                        bool skip = (chunk.vlocs[vlocIndx].v1 & 0x8000) == 0x8000;

                        if (v < numVerts && stripIdx < vstrip.Length)
                        {
                            vstrip[stripIdx] = skip ? (v | 0x8000) : v;
                        }
                    }
                    int numExtraVlocs = chunk.extraVlocs[0];
                    for (int extraVloc = 0; extraVloc < numExtraVlocs; ++extraVloc)
                    {
                        int idx = extraVloc * 4 + 4;
                        int stripIndxSrc = (chunk.extraVlocs[idx] & 0x1FF) / regsPerVertex;
                        int stripIndxDest = (chunk.extraVlocs[idx + 1] & 0x1FF) / regsPerVertex; ;
                        vstrip[stripIndxDest] = (chunk.extraVlocs[idx + 1] & 0x8000) | (vstrip[stripIndxSrc] & 0x1FF);

                        stripIndxSrc = (chunk.extraVlocs[idx + 2] & 0x1FF) / regsPerVertex;
                        stripIndxDest = (chunk.extraVlocs[idx + 3] & 0x1FF) / regsPerVertex; ;
                        vstrip[stripIndxDest] = (chunk.extraVlocs[idx + 3] & 0x8000) | (vstrip[stripIndxSrc] & 0x1FF);
                    }
                    int triIdx = 0;
                    for (int i = 2; i < vstrip.Length; ++i)
                    {
                        int vidx1 = vstart + (vstrip[i - 2] & 0xFF);
                        int vidx2 = vstart + (vstrip[i - 1] & 0xFF);
                        int vidx3 = vstart + (vstrip[i] & 0xFF);

                        int uv1 = i - 2;
                        int uv2 = i - 1;

                        // Flip the faces (indices 1 and 2) to keep the winding rule consistent.
                        if ((triIdx & 1) == 1)
                        {
                            int temp = uv1;
                            uv1 = uv2;
                            uv2 = temp;

                            temp = vidx1;
                            vidx1 = vidx2;
                            vidx2 = temp;
                        }

                        if ((vstrip[i] & 0x8000) == 0)
                        {
                            triangleIndices.Add(vidx1);
                            triangleIndices.Add(vidx2);
                            triangleIndices.Add(vidx3);

                            triangleIndices.Add(vidx2);
                            triangleIndices.Add(vidx1);
                            triangleIndices.Add(vidx3);

                            double udiv = texture.PixelWidth * 16.0;
                            double vdiv = texture.PixelHeight * 16.0;

                            uvCoords[vidx1] = new Point(chunk.uvs[uv1].u / udiv, chunk.uvs[uv1].v / vdiv);
                            uvCoords[vidx2] = new Point(chunk.uvs[uv2].u / udiv, chunk.uvs[uv2].v / vdiv);
                            uvCoords[vidx3] = new Point(chunk.uvs[i].u / udiv, chunk.uvs[i].v / vdiv);
                        }
                        ++triIdx;
                    }
                    vstart += chunk.vertices.Count;
                    ++chunkNo;
                }
                ++meshNo;
            }
            mesh.TriangleIndices = triangleIndices;
            mesh.Positions = positions;
            mesh.TextureCoordinates = new PointCollection(uvCoords);
            mesh.Normals = normals;
            model.Geometry = mesh;
            DiffuseMaterial dm = new DiffuseMaterial();
            dm.Brush = new ImageBrush(texture);
            model.Material = dm;
            return model;
        }

        private class Vertex
        {
            public short x;
            public short y;
            public short z;
        }

        private class ByteVector
        {
            public byte x;
            public byte y;
            public byte z;
        }

        private class VLoc
        {
            public int v1;
            public int v2;
            public int v3;

            public override String ToString()
            {
                return HexUtil.formatHexUShort(v1) + ", " + HexUtil.formatHexUShort(v2) + ", " + HexUtil.formatHexUShort(v3);
            }
        }

        private class UV
        {
            public UV(short u, short v)
            {
                this.u = u;
                this.v = v;
            }

            public short u;
            public short v;
        }

        private class Chunk
        {
            public int mscalID = 0;
            public GIFTag gifTag0 = null;
            public GIFTag gifTag1 = null;
            public List<Vertex> vertices = new List<Vertex>();
            public List<ByteVector> normals = new List<ByteVector>();
            public List<VLoc> vlocs = new List<VLoc>();
            public List<UV> uvs = new List<UV>();
            public byte[] data_4x8;
            public ushort[] extraVlocs;
        }

        private const int NOP_CMD = 0;
        private const int STCYCL_CMD = 1;
        private const int ITOP_CMD = 4;
        private const int STMOD_CMD = 5;
        private const int MSCAL_CMD = 0x14;
        private const int STMASK_CMD = 0x20;

        private static List<Chunk> ReadVerts(ILogger log, byte[] fileData, int offset, int endOffset)
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
                        Debug.WriteLine(HexUtil.formatHex(offset) + " ");
                        Debug.WriteLine("NOP");
                        offset += 4;
                        break;
                    case STCYCL_CMD:
                        Debug.WriteLine(HexUtil.formatHex(offset) + " ");
                        Debug.WriteLine("STCYCL: WL: " + (immCommand >> 8) + " CL: " + (immCommand & 0xFF));
                        offset += 4;
                        break;
                    case ITOP_CMD:
                        Debug.WriteLine(HexUtil.formatHex(offset) + " ");
                        Debug.WriteLine("ITOP: " + immCommand);
                        offset += 4;
                        break;
                    case STMOD_CMD:
                        Debug.WriteLine(HexUtil.formatHex(offset) + " ");
                        Debug.WriteLine("STMOD: " + immCommand);
                        offset += 4;
                        break;
                    case MSCAL_CMD:
                        Debug.WriteLine(HexUtil.formatHex(offset) + " ");
                        Debug.WriteLine("MSCAL: " + immCommand);
                        if (immCommand != 66 && immCommand != 68) {
                            Debug.WriteLine("**** Microcode " + immCommand + " not supported");
                        }
                        currentChunk.mscalID = immCommand;
                        chunks.Add(currentChunk);
                        previousChunk = currentChunk;
                        currentChunk = new Chunk();

                        offset += 4;
                        break;
                    case STMASK_CMD:
                        Debug.WriteLine(HexUtil.formatHex(offset) + " ");
                        offset += 4;
                        int stmask = DataUtil.getLEInt(fileData, offset);
                        Debug.WriteLine("STMASK: " + stmask);
                        offset += 4;
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

                            Debug.WriteLine(HexUtil.formatHex(offset) + " ");
                            Debug.WriteLine("UNPACK: vn: " + vn + ", vl: " + vl + ", Addr: " + addr);
                            Debug.WriteLine(", num: " + numCommand);
                            if (flag) {
                                Debug.WriteLine(", Flag");
                            }
                            if (usn) {
                                Debug.WriteLine(", Unsigned");
                            }
                            if (mask) {
                                Debug.WriteLine(", Mask");
                            }
                            Debug.WriteLine("");
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
                                    ByteVector vec = new ByteVector();
                                    vec.x = fileData[idx++];
                                    vec.y = fileData[idx++];
                                    vec.z = fileData[idx++];
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
                                } else if (2 == numCommand) {
                                    currentChunk.gifTag0 = new GIFTag();
                                    currentChunk.gifTag0.parse(fileData, offset);
                                    currentChunk.gifTag1 = new GIFTag();
                                    currentChunk.gifTag1.parse(fileData, offset + 16);
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
                                currentChunk.data_4x8 = new byte[numBytes];
                                for (int i = 0; i < numBytes; ++i) {
                                    currentChunk.data_4x8[i] = fileData[offset + i];
                                }
                                if (numCommand != 1) {
                                    log.LogLine("Unsupported data - only 1 line expected");
                                    log.LogLine("v4-8 data. " + numBytes + " bytes, addr=" + addr);
                                    for (int i = 0; i < numBytes; i += 4) {
                                        log.LogLine("0x" + currentChunk.data_4x8[i].ToString("x2") + ", 0x" + 
                                            currentChunk.data_4x8[i+1].ToString("x2") + ", 0x" +
                                            currentChunk.data_4x8[i+2].ToString("x2") + ", 0x" +
                                            currentChunk.data_4x8[i+3].ToString("x2")
                                            );
                                    }
                                }
                                offset += numBytes;
                            } else {
                                Debug.WriteLine("Unknown vnvl combination: vn=" + vn + ", vl=" + vl);
                                offset = endOffset;
                            }
                        } else {
                            Debug.WriteLine("Unknown command: " + vifCommand);
                            offset = endOffset;
                        }
                        break;
                }
            }
            return chunks;
        }
    }
}

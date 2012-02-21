using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;

namespace WorldExplorer.DataLoaders
{
    class VifDecoder
    {
        public static Model3D Decode(byte[] data, int startOffset, int length, BitmapSource texture)
        {
            int numMeshes = data[startOffset + 0x12] & 0xFF;
            int offset1 = DataUtil.getLEInt(data, startOffset + 0x24);
            int offsetVerts = DataUtil.getLEInt(data, startOffset + 0x28);
            int offsetEndVerts = DataUtil.getLEInt(data, startOffset + 0x2C);

            var chunks = ReadVerts(data, startOffset + offsetVerts, startOffset + offsetEndVerts);
            return CreateModel3D(chunks, texture);
        }

        private static Model3D CreateModel3D(List<Chunk> chunks, BitmapSource texture)
        {
            GeometryModel3D model = new GeometryModel3D();
            var mesh = new MeshGeometry3D();

            int numVertices = 0;
            foreach (Chunk chunk in chunks) {
                numVertices += chunk.vertices.Count;
            }
            var triangleIndices = new Int32Collection();
            var uvCoords = new Point[numVertices];
            int vstart = 0;
            int uvstart = 0;
            foreach (Chunk chunk in chunks) {
                foreach (Vertex vertex in chunk.vertices) {
                    mesh.Positions.Add(new Point3D(vertex.x / 127.0, vertex.y / 127.0, vertex.z / 127.0));
                }
                int[] vstrip = new int[chunk.gifTag0.nloop];
                int regsPerVertex = chunk.gifTag0.nreg;
                int numVlocs = chunk.vlocs.Count;
                int numVerts = chunk.vertices.Count;
                for (int vlocIndx = 2; vlocIndx < numVlocs; ++vlocIndx) {
                    int v = vlocIndx - 2;
                    int stripIdx2 = (chunk.vlocs[vlocIndx].v2 & 0xFF) / regsPerVertex;
                    int stripIdx3 = (chunk.vlocs[vlocIndx].v3 & 0xFF) / regsPerVertex;
                    if (stripIdx3 < vstrip.Length && stripIdx2 < vstrip.Length) {
                        vstrip[stripIdx3] = vstrip[stripIdx2] & 0xFF;

                        bool skip2 = (chunk.vlocs[vlocIndx].v3 & 0x8000) == 0x8000;
                        if (skip2) {
                            vstrip[stripIdx3] |= 0x8000;
                        }
                    }
                    int stripIdx = (chunk.vlocs[vlocIndx].v1 & 0xFF) / regsPerVertex;
                    bool skip = (chunk.vlocs[vlocIndx].v1 & 0x8000) == 0x8000;

                    if (v >= 0 && v < numVerts && stripIdx < vstrip.Length) {
                        vstrip[stripIdx] = skip ? (v | 0x8000) : v;
                    }
                }
                int triIdx = 0;
                for (int i = 2; i < vstrip.Length; ++i) {
                    int vidx1 = vstart + (vstrip[i - 2] & 0xFF);
                    int vidx2 = vstart + (vstrip[i - 1] & 0xFF);
                    int vidx3 = vstart + (vstrip[i] & 0xFF);

                    int uv1 = i - 2;
                    int uv2 = i - 1;

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
                        triangleIndices.Add(vidx1);
                        triangleIndices.Add(vidx2);
                        triangleIndices.Add(vidx3);

                        triangleIndices.Add(vidx3);
                        triangleIndices.Add(vidx2);
                        triangleIndices.Add(vidx1);

                        double udiv = texture.PixelWidth * 16.0;
                        double vdiv = texture.PixelHeight * 16.0;

                        uvCoords[vidx1] = new Point(chunk.uvs[uv1].u /udiv, chunk.uvs[uv1].v / vdiv);
                        uvCoords[vidx2] = new Point(chunk.uvs[uv2].u / udiv, chunk.uvs[uv2].v / vdiv);
                        uvCoords[vidx3] = new Point(chunk.uvs[i].u / udiv, chunk.uvs[i].v / vdiv);

                        ++triIdx;
                    } else {
                        triIdx = 0;
                    }
                }
                vstart += chunk.vertices.Count;
                uvstart += chunk.uvs.Count;
            }
            mesh.TriangleIndices = triangleIndices;
            mesh.TextureCoordinates = new PointCollection(uvCoords);
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
        }

        private const int NOP_CMD = 0;
        private const int STCYCL_CMD = 1;
        private const int ITOP_CMD = 4;
        private const int STMOD_CMD = 5;
        private const int MSCAL_CMD = 0x14;
        private const int STMASK_CMD = 0x20;

        private static List<Chunk> ReadVerts(byte[] fileData, int offset, int endOffset)
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
                                if (1 == numCommand) {
                                    currentChunk.gifTag0 = new GIFTag();
                                    currentChunk.gifTag0.parse(fileData, offset);
                                } else if (2 == numCommand) {
                                    currentChunk.gifTag0 = new GIFTag();
                                    currentChunk.gifTag0.parse(fileData, offset);
                                    currentChunk.gifTag1 = new GIFTag();
                                    currentChunk.gifTag1.parse(fileData, offset + 16);
                                }
                                int numBytes = numCommand * 16;
                                offset += numBytes;
                            } else if (vn == 3 && vl == 1) {
                                // v4-16
                                int numBytes = numCommand * 8;
                                offset += numBytes;
                            } else if (vn == 3 && vl == 2) {
                                // v4-8
                                int numBytes = numCommand * 4;
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

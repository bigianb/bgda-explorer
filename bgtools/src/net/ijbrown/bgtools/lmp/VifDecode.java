/*  Copyright (C) 2011 Ian Brown

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
package net.ijbrown.bgtools.lmp;

import java.io.*;
import java.text.DecimalFormat;
import java.util.ArrayList;
import java.util.List;

/**
 * Decodes a VIF model file.
 */
public class VifDecode
{
    public static void main(String[] args) throws IOException
    {
        String outDir = "/emu/bgda/BG/DATA_extracted/";

        File outDirFile = new File(outDir);
        outDirFile.mkdirs();

        VifDecode obj = new VifDecode();
        obj.extract("barrel", outDirFile, 264, 128);
        obj = new VifDecode();
        obj.extract("snowflag", outDirFile, 32, 128);
        obj = new VifDecode();
        obj.extract("lever", outDirFile, 16, 128);
        obj = new VifDecode();
        obj.extract("chest_large", outDirFile, 16, 128);
        obj = new VifDecode();
        obj.extract("w_bands", outDirFile, 214, 150);
        obj = new VifDecode();
        obj.extract("book", outDirFile, 169, 64);
    }

    public void extract(String name, File outDir, int texw, int texh) throws IOException
    {
        File file = new File(outDir, name + ".vif");
        BufferedInputStream is = new BufferedInputStream(new FileInputStream(file));

        int fileLength = (int) file.length();
        byte[] fileData = new byte[fileLength];

        int offset = 0;
        int remaining = fileLength;
        while (remaining > 0) {
            int read = is.read(fileData, offset, remaining);
            if (read == -1) {
                throw new IOException("Read less bytes then expected when reading file");
            }
            remaining -= read;
            offset += read;
        }

        int numMeshes = fileData[0x12] & 0xFF;
        int offset1 = DataUtil.getLEInt(fileData, 0x24);
        int offsetVerts = DataUtil.getLEInt(fileData, 0x28);
        int offsetEndVerts = DataUtil.getLEInt(fileData, 0x2C);

        readVerts(fileData, offsetVerts, offsetEndVerts);

        writeObj(name, outDir, texw, texh, 128.0);
    }

    private void writeMtlFile(File mtlFile, String name) throws IOException
    {
        PrintWriter writer = new PrintWriter(mtlFile);
        writer.println("newmtl " + name);
        writer.println("map_Kd " + name + ".tex.png");
        writer.close();
    }

    public void writeObj(String name, File dir, double texWidth, double texHeight, double scale) throws IOException
    {
        DecimalFormat df = new DecimalFormat("0.0000");

        writeMtlFile(new File(dir, name + ".mtl"), name);

        File objFile = new File(dir, name + ".obj");
        PrintWriter writer = new PrintWriter(objFile);

        int vstart = 1;
        int uvstart = 1;
        int chunkNo = 1;
        writer.println("mtllib " + name + ".mtl");
        writer.println("usemtl " + name);
        for (Chunk chunk : chunks) {
            writer.println("# Chunk " + chunkNo++);
            writer.println("# GifTag: " + chunk.gifTag0.toString());
            if (chunk.gifTag1 != null){
                writer.println("# GifTag1: " + chunk.gifTag1.toString());
            }

            int regsPerVertex = chunk.gifTag0.nreg;
            if (chunk.uvs.size() != chunk.gifTag0.nloop) {
                throw new RuntimeException("Expected " + chunk.gifTag0.nloop + " uvs but found " + chunk.uvs.size());
            }
            int[] vstrip = new int[chunk.gifTag0.nloop];

            for (Vertex vertex : chunk.vertices) {
                writer.write("v ");
                writer.print(df.format(vertex.x / scale));
                writer.write(" ");
                writer.print(df.format(vertex.y / scale));
                writer.write(" ");
                writer.print(df.format(vertex.z / scale));
                writer.println();
            }
            int numVlocs = chunk.vlocs.size();
            int numVerts = chunk.vertices.size();
            for (int vlocIndx = 2; vlocIndx < numVlocs; ++vlocIndx) {
                int v = vlocIndx - 2;
                int stripIdx2 = (chunk.vlocs.get(vlocIndx).v2 & 0xFF) / regsPerVertex;
                int stripIdx3 = (chunk.vlocs.get(vlocIndx).v3 & 0xFF) / regsPerVertex;
                if (stripIdx3 < vstrip.length && stripIdx2 < vstrip.length) {
                    vstrip[stripIdx3] = vstrip[stripIdx2] & 0xFF;

                    boolean skip2 = (chunk.vlocs.get(vlocIndx).v3 & 0x8000) == 0x8000;
                    if (skip2) {
                        vstrip[stripIdx3] |= 0x8000;
                    }
                }
                int stripIdx = (chunk.vlocs.get(vlocIndx).v1 & 0xFF) / regsPerVertex;
                boolean skip = (chunk.vlocs.get(vlocIndx).v1 & 0x8000) == 0x8000;

                if (v >= 0 && v < numVerts && stripIdx < vstrip.length) {
                    vstrip[stripIdx] = skip ? (v | 0x8000) : v;
                }
            }

            for (UV uv : chunk.uvs) {
                writer.write("vt ");
                writer.print(df.format(uv.u / (16.0 * texWidth)));
                writer.write(" ");
                writer.println(df.format(1.0 - uv.v / (16.0 * texHeight)));
            }

            for (ByteVector vec : chunk.normals) {
                writer.write("vn ");
                writer.print(df.format(vec.x / 127.0));
                writer.write(" ");
                writer.print(df.format(vec.y / 127.0));
                writer.write(" ");
                writer.println(df.format(vec.z / 127.0));
            }

            int triIdx = 0;
            for (int i = 2; i < vstrip.length; ++i) {
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
                    writer.write("f ");
                    writer.print(vidx1);
                    writer.write("/");
                    writer.print(uvstart + uv1);
                    writer.write("/");
                    writer.print(vidx1);
                    writer.write(" ");
                    writer.print(vidx2);
                    writer.write("/");
                    writer.print(uvstart + uv2);
                    writer.write("/");
                    writer.print(vidx2);
                    writer.write(" ");
                    writer.print(vidx3);
                    writer.write("/");
                    writer.print(uvstart + i);
                    writer.write("/");
                    writer.println(vidx3);
                    ++triIdx;
                } else {
                    ++triIdx;
                }
            }



            vstart += chunk.vertices.size();
            uvstart += chunk.uvs.size();
        }
        writer.close();
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

        @Override
        public String toString()
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
        public int mscalID=0;
        public GIFTag gifTag0 = null;
        public GIFTag gifTag1 = null;
        public List<Vertex> vertices = new ArrayList<Vertex>();
        public List<ByteVector> normals = new ArrayList<ByteVector>();
        public List<VLoc> vlocs = new ArrayList<VLoc>();
        public List<UV> uvs = new ArrayList<UV>();
    }

    private Chunk currentChunk = null;
    private Chunk previousChunk = null;
    private List<Chunk> chunks = new ArrayList<Chunk>();

    private static final int NOP_CMD = 0;
    private static final int STCYCL_CMD = 1;
    private static final int ITOP_CMD = 4;
    private static final int STMOD_CMD = 5;
    private static final int MSCAL_CMD = 0x14;
    private static final int STMASK_CMD = 0x20;

    public void readVerts(byte[] fileData, int offset, int endOffset)
    {
        currentChunk = new Chunk();
        while (offset < endOffset) {
            int vifCommand = fileData[offset + 3] & 0x7f;
            int numCommand = fileData[offset + 2] & 0xff;
            int immCommand = DataUtil.getLEShort(fileData, offset);
            switch (vifCommand) {
                case NOP_CMD:
                    System.out.print(HexUtil.formatHex(offset) + " ");
                    System.out.println("NOP");
                    offset += 4;
                    break;
                case STCYCL_CMD:
                    System.out.print(HexUtil.formatHex(offset) + " ");
                    System.out.println("STCYCL: WL: " + (immCommand >> 8) + " CL: " + (immCommand & 0xFF));
                    offset += 4;
                    break;
                case ITOP_CMD:
                    System.out.print(HexUtil.formatHex(offset) + " ");
                    System.out.println("ITOP: " + immCommand);
                    offset += 4;
                    break;
                case STMOD_CMD:
                    System.out.print(HexUtil.formatHex(offset) + " ");
                    System.out.println("STMOD: " + immCommand);
                    offset += 4;
                    break;
                case MSCAL_CMD:
                    System.out.print(HexUtil.formatHex(offset) + " ");
                    System.out.println("MSCAL: " + immCommand);
                    if (immCommand != 66 && immCommand != 68){
                        System.out.println("**** Microcode " + immCommand + " not supported");
                    }
                    currentChunk.mscalID = immCommand;
                    chunks.add(currentChunk);
                    previousChunk = currentChunk;
                    currentChunk = new Chunk();

                    offset += 4;
                    break;
                case STMASK_CMD:
                    System.out.print(HexUtil.formatHex(offset) + " ");
                    offset += 4;
                    int stmask = DataUtil.getLEInt(fileData, offset);
                    System.out.println("STMASK: " + stmask);
                    offset += 4;
                    break;
                default:
                    if ((vifCommand & 0x60) == 0x60) {
                        // unpack command
                        boolean mask = ((vifCommand & 0x10) == 0x10);
                        int vn = (vifCommand >> 2) & 3;
                        int vl = vifCommand & 3;
                        int addr = immCommand & 0x1ff;
                        boolean flag = (immCommand & 0x8000) == 0x8000;
                        boolean usn = (immCommand & 0x4000) == 0x4000;

                        System.out.print(HexUtil.formatHex(offset) + " ");
                        System.out.print("UNPACK: vn: " + vn + ", vl: " + vl + ", Addr: " + addr);
                        System.out.print(", num: " + numCommand);
                        if (flag) {
                            System.out.print(", Flag");
                        }
                        if (usn) {
                            System.out.print(", Unsigned");
                        }
                        if (mask) {
                            System.out.print(", Mask");
                        }
                        System.out.println("");
                        offset += 4;
                        if (vn == 1 && vl == 1) {
                            // v2-16
                            // I don't know why but the UVs come after the MSCAL instruction.
                            if (previousChunk != null) {
                                for (int uvnum = 0; uvnum < numCommand; ++uvnum) {
                                    short u = DataUtil.getLEShort(fileData, offset);
                                    short v = DataUtil.getLEShort(fileData, offset + 2);
                                    previousChunk.uvs.add(new UV(u, v));
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
                                    currentChunk.vertices.add(vertex);
                                } else {
                                    int x = DataUtil.getLEUShort(fileData, offset);
                                    int y = DataUtil.getLEUShort(fileData, offset + 2);
                                    int z = DataUtil.getLEUShort(fileData, offset + 4);
                                    offset += 6;

                                    VLoc vloc = new VLoc();
                                    vloc.v1 = x;
                                    vloc.v2 = y;
                                    vloc.v3 = z;
                                    currentChunk.vlocs.add(vloc);
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
                                currentChunk.normals.add(vec);
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
                            System.out.println("Unknown vnvl combination: vn=" + vn + ", vl=" + vl);
                            offset = endOffset;
                        }
                    } else {
                        System.out.println("Unknown command: " + vifCommand);
                        offset = endOffset;
                    }
                    break;
            }
        }
    }
}

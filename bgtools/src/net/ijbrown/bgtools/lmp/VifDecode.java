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
import java.util.ArrayList;
import java.util.List;

/**
 * Decodes a VIF model file.
 */
public class VifDecode
{
    public static void main(String[] args) throws IOException
    {
        String filename = "/emu/bgda/BG/DATA_extracted/lever.vif";

        String outDir = "/emu/bgda/BG/DATA_extracted/";

        File outDirFile = new File(outDir);
        outDirFile.mkdirs();

        VifDecode obj = new VifDecode();
        obj.extract(filename, outDirFile);
    }

    private void extract(String filename, File outDir) throws IOException
    {
        File file = new File(filename);
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
        System.out.println("Read " + vertices.size() + " vertices");

        if (gifTag0 != null) {
            System.out.println(gifTag0.toString());
        }

        File objFile = new File(outDir, "barrel.obj");
        writeObj(vertices, objFile);
    }

    private void writeObj(List<Vertex> vertices, File objFile) throws IOException
    {
        PrintWriter writer = new PrintWriter(objFile);
        for (Vertex vertex : vertices) {
            writer.write("v ");
            writer.print(vertex.x / 100.0);
            writer.write(" ");
            writer.print(vertex.y / 100.0);
            writer.write(" ");
            writer.print(vertex.z / 100.0);
            writer.println();
        }
        int v = 1;
        int numVerts = vertices.size();
        while (v < numVerts - 1) {
            writer.write("f ");
            writer.print(v);
            writer.write(" ");
            writer.print(v + 1);
            writer.write(" ");
            writer.print(v + 2);
            writer.println();
            ++v;
        }

        for (Face face : faces) {
            writer.write("# face ");
            writer.write(HexUtil.formatHexUShort(face.v1));
            writer.write(" ");
            writer.write(HexUtil.formatHexUShort(face.v2));
            writer.write(" ");
            writer.write(HexUtil.formatHexUShort(face.v3));
            writer.println();
        }

        for (ByteVector vec : normals) {
            writer.write("# n ");
            writer.print((int) vec.x);
            writer.write(" ");
            writer.print((int) vec.y);
            writer.write(" ");
            writer.print((int) vec.z);
            writer.println();
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

    private class Face
    {
        public int v1;
        public int v2;
        public int v3;
    }

    GIFTag gifTag0 = null;

    private List<Vertex> vertices = new ArrayList<Vertex>();
    private List<ByteVector> normals = new ArrayList<ByteVector>();
    private List<Face> faces = new ArrayList<Face>();

    private static final int NOP_CMD = 0;
    private static final int STCYCL_CMD = 1;
    private static final int ITOP_CMD = 4;
    private static final int MSCAL_CMD = 0x14;
    private static final int STMASK_CMD = 0x20;

    private void readVerts(byte[] fileData, int offset, int endOffset)
    {
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
                case MSCAL_CMD:
                    System.out.print(HexUtil.formatHex(offset) + " ");
                    System.out.println("MSCAL: " + immCommand);

                    if (gifTag0 != null) {
                        System.out.println(gifTag0.toString());
                    }
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
                            int numBytes = numCommand * 4;
                            offset += numBytes;
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
                                    vertices.add(vertex);
                                } else {
                                    int x = DataUtil.getLEUShort(fileData, offset);
                                    int y = DataUtil.getLEUShort(fileData, offset + 2);
                                    int z = DataUtil.getLEUShort(fileData, offset + 4);
                                    offset += 6;

                                    Face face = new Face();
                                    face.v1 = x;
                                    face.v2 = y;
                                    face.v3 = z;
                                    faces.add(face);
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
                                normals.add(vec);
                            }
                            int numBytes = ((numCommand * 3) + 3) & ~3;
                            offset += numBytes;
                        } else if (vn == 3 && vl == 0) {
                            // v4-32
                            if (1 == numCommand) {
                                gifTag0 = new GIFTag();
                                gifTag0.parse(fileData, offset);
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

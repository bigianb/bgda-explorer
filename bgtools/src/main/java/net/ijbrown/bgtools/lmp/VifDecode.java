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
        GameType gameType = GameType.JUSTICE_LEAGUE_HEROES;

        Config config = new Config(gameType);
        String inDir = config.getDataDir();
        String outDir = inDir+"../DATA_extracted/";

        VifDecode obj = new VifDecode();
        obj.extract("tophat", new File(outDir + "ZATANNA_LMP"), 264, 128);
        //obj.extract("superman", new File(outDir + "SUPERMAN_LMP"), 264, 128);
    }

    public List<Mesh> decode(byte[] data, int startOffset)
    {
        int numMeshes = data[0x12+startOffset] & 0xFF;
        int meshBlockOffset = 0x28;
        if (0 == numMeshes)
        {
            numMeshes = 1;
            meshBlockOffset = 0x68;
        }
        //int offset1 = DataUtil.getLEInt(data, 0x24+startOffset);
        List<Mesh> meshes = new ArrayList<>();

        for (int meshNum = 0; meshNum < numMeshes; ++meshNum) {
            int offsetVerts = DataUtil.getLEInt(data, startOffset + meshBlockOffset + meshNum * 4);
            int offsetEndVerts = DataUtil.getLEInt(data, startOffset + meshBlockOffset + 4 + meshNum * 4);
            var chunks = readVerts(data, startOffset + offsetVerts, startOffset + offsetEndVerts);
            var mesh = ChunksToMesh(chunks);
            meshes.add(mesh);
        }

        return meshes;
    }

    // Finds which vertex weight object to use for the given vertex.
    private static VertexWeight FindVertexWeight(List<VertexWeight> weights, int vertexNum)
    {
        for (var weight : weights) {
            if (vertexNum >= weight.startVertex && vertexNum <= weight.endVertex) {
                return weight;
            }
        }
        if (weights.size() != 0) {
            System.out.println("Failed to find vertex weight");
        }
        return new VertexWeight();
    }

    public static Mesh ChunksToMesh(List<Chunk> chunks)
    {
        Mesh mesh = new Mesh();
        int numVertices = 0;
        for (Chunk chunk : chunks) {
            numVertices += chunk.vertices.size();
        }

        for(var i=0; i<numVertices; ++i){
            mesh.uvCoords.add(null);
        }

        int vstart = 0;
        for (var chunk : chunks) {
            if (null == chunk.gifTag0)
            {
                // Hack to deal with JLH models. TODO: Fix this properly
                continue;
            }
            if ((chunk.gifTag0.prim & 0x07) != 4) {
                throw new RuntimeException("Can only deal with tri strips");
            }

            for (var vertex : chunk.vertices) {
                var point = new Vec3F(vertex.x / 16.0f, vertex.y / 16.0f, vertex.z / 16.0f);
                mesh.vertices.add(point);
            }
            for (var normal : chunk.normals) {
                mesh.normals.add(new Vec3F(normal.x / 127.0f, normal.y / 127.0f, normal.z / 127.0f));
            }

            for (VertexWeight vw : chunk.vertexWeights) {
                VertexWeight vwAdjusted = new VertexWeight(vw);
                vwAdjusted.startVertex += vstart;
                if (vwAdjusted.endVertex >= chunk.vertices.size()) {
                    vwAdjusted.endVertex = chunk.vertices.size() - 1;
                }
                vwAdjusted.endVertex += vstart;
                if (vw.startVertex <= (chunk.vertices.size() - 1)) {
                    mesh.vertexWeights.add(vwAdjusted);
                }
            }

            int[] vstrip = new int[chunk.gifTag0.nloop];
            int regsPerVertex = chunk.gifTag0.nreg;
            int numVlocs = chunk.vlocs.size();
            int numVertsInChunk = chunk.vertices.size();
            for (int vlocIndx = 2; vlocIndx < numVlocs; ++vlocIndx) {
                int v = vlocIndx - 2;
                int stripIdx2 = (chunk.vlocs.get(vlocIndx).v2 & 0x1FF) / regsPerVertex;
                int stripIdx3 = (chunk.vlocs.get(vlocIndx).v3 & 0x1FF) / regsPerVertex;
                if (stripIdx3 < vstrip.length && stripIdx2 < vstrip.length) {
                    vstrip[stripIdx3] = vstrip[stripIdx2] & 0x1FF;

                    boolean skip2 = (chunk.vlocs.get(vlocIndx).v3 & 0x8000) == 0x8000;
                    if (skip2) {
                        vstrip[stripIdx3] |= 0x8000;
                    }
                }
                int stripIdx = (chunk.vlocs.get(vlocIndx).v1 & 0x1FF) / regsPerVertex;
                boolean skip = (chunk.vlocs.get(vlocIndx).v1 & 0x8000) == 0x8000;

                if (v < numVertsInChunk && stripIdx < vstrip.length) {
                    vstrip[stripIdx] = skip ? (v | 0x8000) : v;
                }
            }

            int numExtraVlocs = chunk.extraVlocs[0];
            for (int extraVloc = 0; extraVloc < numExtraVlocs; ++extraVloc) {
                int idx = extraVloc * 4 + 4;
                int stripIndxSrc = (chunk.extraVlocs[idx] & 0x1FF) / regsPerVertex;
                int stripIndxDest = (chunk.extraVlocs[idx + 1] & 0x1FF) / regsPerVertex;
                vstrip[stripIndxDest] = (chunk.extraVlocs[idx + 1] & 0x8000) | (vstrip[stripIndxSrc] & 0x1FF);

                stripIndxSrc = (chunk.extraVlocs[idx + 2] & 0x1FF) / regsPerVertex;
                stripIndxDest = (chunk.extraVlocs[idx + 3] & 0x1FF) / regsPerVertex;
                vstrip[stripIndxDest] = (chunk.extraVlocs[idx + 3] & 0x8000) | (vstrip[stripIndxSrc] & 0x1FF);
            }

            int triIdx = 0;
            for (int i = 2; i < vstrip.length; ++i) {
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

                    var p1 = chunk.uvs.get(uv1);
                    var p2 = chunk.uvs.get(uv2);
                    var p3 = chunk.uvs.get(uv3);

                    if (mesh.uvCoords.get(vidx1) != null && !mesh.uvCoords.get(vidx1).equals(p1))
                    {
                        // There is more than 1 uv assignment to this vertex, so we need to duplicate it.
                        int originalVIdx = vidx1;
                        vidx1 = vstart + numVertsInChunk;
                        numVertsInChunk++;
                        mesh.vertices.add(mesh.vertices.get(originalVIdx));
                        mesh.normals.add(mesh.normals.get(originalVIdx));
                        mesh.uvCoords.add(null);

                        var weight = FindVertexWeight(chunk.vertexWeights, originalVIdx - vstart);
                        if (weight.boneWeight1 > 0)
                        {
                            var vw = new VertexWeight(weight);
                            vw.startVertex = vidx1;
                            vw.endVertex = vidx1;
                            mesh.vertexWeights.add(vw);
                        }

                    }
                    if (mesh.uvCoords.get(vidx2) != null && !mesh.uvCoords.get(vidx2).equals(p2))
                    {
                        // There is more than 1 uv assignment to this vertex, so we need to duplicate it.
                        int originalVIdx = vidx2;
                        vidx2 = vstart + numVertsInChunk;
                        numVertsInChunk++;
                        mesh.vertices.add(mesh.vertices.get(originalVIdx));
                        mesh.normals.add(mesh.normals.get(originalVIdx));
                        mesh.uvCoords.add(null);

                        var weight = FindVertexWeight(chunk.vertexWeights, originalVIdx - vstart);
                        if (weight.boneWeight1 > 0)
                        {
                            var vw = new VertexWeight(weight);
                            vw.startVertex = vidx2;
                            vw.endVertex = vidx2;
                            mesh.vertexWeights.add(vw);
                        }
                    }
                    if (mesh.uvCoords.get(vidx3) != null && !mesh.uvCoords.get(vidx3).equals(p3))
                    {
                        // There is more than 1 uv assignment to this vertex, so we need to duplicate it.
                        int originalVIdx = vidx3;
                        vidx3 = vstart + numVertsInChunk;
                        numVertsInChunk++;
                        mesh.vertices.add(mesh.vertices.get(originalVIdx));
                        mesh.normals.add(mesh.normals.get(originalVIdx));
                        mesh.uvCoords.add(null);

                        var weight = FindVertexWeight(chunk.vertexWeights, originalVIdx - vstart);
                        if (weight.boneWeight1 > 0)
                        {
                            var vw = new VertexWeight(weight);
                            vw.startVertex = vidx3;
                            vw.endVertex = vidx3;
                            mesh.vertexWeights.add(vw);
                        }
                    }

                    mesh.uvCoords.set(vidx1, p1);
                    mesh.uvCoords.set(vidx2, p2);
                    mesh.uvCoords.set(vidx3, p3);

                    mesh.triangleIndices.add(vidx1);
                    mesh.triangleIndices.add(vidx2);
                    mesh.triangleIndices.add(vidx3);
                }
                ++triIdx;
            }
            vstart += numVertsInChunk;
        }
        return mesh;
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

        List<Mesh> meshes = decode(fileData, 0);

    }

    public static class Vertex
    {
        public short x;
        public short y;
        public short z;
    }

    public static class Vec3F
    {
        public Vec3F(float x, float y, float z){
            this.x=x; this.y=y; this.z=z;
        }

        public float x;
        public float y;
        public float z;
    }

    public static class ByteVector
    {
        public byte x;
        public byte y;
        public byte z;
    }

    public static class VLoc
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

    public static class UV
    {
        public UV(short u, short v)
        {
            this.u = u;
            this.v = v;
        }

        public short u;
        public short v;
    }

    public static class VertexWeight
    {
        public int startVertex;
        public int endVertex;
        public int bone1;
        public int bone2;
        public int bone3;
        public int bone4;
        public int boneWeight1;
        public int boneWeight2;
        public int boneWeight3;
        public int boneWeight4;

        public VertexWeight(VertexWeight weight) {
            startVertex = weight.startVertex;
            endVertex = weight.endVertex;
            bone1 = weight.bone1;
            bone2 = weight.bone2;
            bone3 = weight.bone3;
            bone4 = weight.bone4;
            boneWeight1 = weight.boneWeight1;
            boneWeight2 = weight.boneWeight2;
            boneWeight3 = weight.boneWeight3;
            boneWeight4 = weight.boneWeight4;
        }

        public VertexWeight() {

        }
    }

    public static class Chunk
    {
        public int mscalID=0;
        public GIFTag gifTag0 = null;
        public GIFTag gifTag1 = null;
        public List<Vertex> vertices = new ArrayList<>();
        public List<ByteVector> normals = new ArrayList<>();
        public List<VLoc> vlocs = new ArrayList<>();
        public List<UV> uvs = new ArrayList<>();
        public List<VertexWeight> vertexWeights = new ArrayList<>();

        public int[] extraVlocs = null;
    }

    public static class Mesh
    {
        public List<Vec3F> vertices = new ArrayList<>();
        public List<Vec3F> normals = new ArrayList<>();
        public List<Integer> triangleIndices = new ArrayList<>();
        public List<UV> uvCoords = new ArrayList<>();
        public List<VertexWeight> vertexWeights = new ArrayList<>();
    }

    private static final int NOP_CMD = 0;
    private static final int STCYCL_CMD = 1;
    private static final int ITOP_CMD = 4;
    private static final int STMOD_CMD = 5;
    private static final int MSCAL_CMD = 0x14;
    private static final int STMASK_CMD = 0x20;

    public List<Chunk> readVerts(byte[] fileData, int offset, int endOffset)
    {
        List<Chunk> chunks = new ArrayList<>();
        Chunk currentChunk = new Chunk();
        Chunk previousChunk = null;
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
                    if (immCommand != 66 && immCommand != 68 && immCommand != 70){
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
                        System.out.println();
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
                            } else {
                                System.out.println("unknown numCommand="+numCommand);
                            }
                            int numBytes = numCommand * 16;
                            offset += numBytes;
                        } else if (vn == 3 && vl == 1) {
                            // v4-16
                            int numBytes = numCommand * 8;
                            int numShorts = numCommand * 4;
                            if (usn) {
                                currentChunk.extraVlocs = new int[numShorts];
                                for (int i = 0; i < numShorts; ++i) {
                                    currentChunk.extraVlocs[i] = DataUtil.getLEUShort(fileData, offset + i * 2);
                                }
                            }
                            offset += numBytes;
                        } else if (vn == 3 && vl == 2) {
                            // v4-8
                            int curVertex=0;
                            for (int i = 0; i < numCommand; ++i) {
                                VertexWeight vw = new VertexWeight();
                                vw.startVertex = curVertex;
                                vw.bone1 = (fileData[offset++] &0xFF )/ 4;
                                vw.boneWeight1 = fileData[offset++] & 0xFF;
                                vw.bone2 = fileData[offset++] & 0xFF;
                                if (vw.bone2 == 0xFF) {
                                    // Single bone
                                    vw.boneWeight2 = 0;
                                    int count = fileData[offset++];
                                    curVertex += count;
                                } else {
                                    vw.bone2 /= 4;
                                    vw.boneWeight2 = fileData[offset++] & 0xFF;
                                    ++curVertex;

                                    if (vw.boneWeight1 + vw.boneWeight2 < 255)
                                    {
                                        ++i;
                                        vw.bone3 = (fileData[offset++] &0xFF) / 4;
                                        vw.boneWeight3 = fileData[offset++] & 0xFF;
                                        vw.bone4 = fileData[offset++] & 0xFF;
                                        int bw4 = fileData[offset++] & 0xFF;
                                        if (vw.bone4 != 255)
                                        {
                                            vw.bone4 /= 4;
                                            vw.boneWeight4 = bw4;
                                        }
                                    }

                                }
                                vw.endVertex = curVertex - 1;
                                currentChunk.vertexWeights.add(vw);
                            }

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
        return chunks;
    }
}

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
        String filename = "/emu/bgda/BG/DATA_extracted/barrel.vif";

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

        List<Vertex> vertices = readVerts(fileData, offsetVerts);

        List<Vertex> vertices2 = readVerts(fileData, 0x6b0);
        vertices.addAll(readVerts(fileData, 0xa90));
        vertices.addAll(readVerts(fileData, 0xe00));
        vertices.addAll(readVerts(fileData, 0x1190));
        vertices.addAll(readVerts(fileData, 0x1500));
        vertices.addAll(readVerts(fileData, 0x1870));
        vertices.addAll(readVerts(fileData, 0x1BE0));
        vertices.addAll(readVerts(fileData, 0x1F90));
        vertices.addAll(readVerts(fileData, 0x2310));
        vertices.addAll(readVerts(fileData, 0x24D0));

        vertices.addAll(vertices2);
        System.out.println("Read " + vertices.size() + " vertices");

        File objFile = new File(outDir, "barrel.obj");
        writeObj(vertices, objFile);
    }

    private void writeObj(List<Vertex> vertices, File objFile) throws IOException
    {
        PrintWriter writer = new PrintWriter(objFile);
        for (Vertex vertex : vertices){
            writer.write("v ");
            writer.print(vertex.x);
            writer.write(" ");
            writer.print(vertex.y);
            writer.write(" ");
            writer.print(vertex.z);
            writer.println();
        }
        int v=1;
        int numVerts = vertices.size();
        while (v < numVerts-1){
            writer.write("f ");
            writer.print(v);
            writer.write(" ");
            writer.print(v+1);
            writer.write(" ");
            writer.print(v+2);
            writer.println();
            ++v;
        }
        writer.close();
    }

    private class Vertex
    {
        public short x;
        public short y;
        public short z;
    }

    private List<Vertex> readVerts(byte[] fileData, int offset)
    {
        List<Vertex> vertices = new ArrayList<Vertex>();

        byte id = fileData[offset];
        int i = offset + 8;

        int numVerts = fileData[offset+6] & 0x0ff;

        int maxIdx = i + 6*numVerts;
        while (i < maxIdx){
            short x = DataUtil.getLEShort(fileData, i);
            short y = DataUtil.getLEShort(fileData, i+2);
            short z = DataUtil.getLEShort(fileData, i+4);
            
            i += 6;

            Vertex vertex = new Vertex();
            vertex.x = x;
            vertex.y = y;
            vertex.z = z;
            vertices.add(vertex);
        }

        return vertices;
    }
}

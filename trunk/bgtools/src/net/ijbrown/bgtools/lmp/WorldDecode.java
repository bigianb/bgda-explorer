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
 * Decodes an xxx.world file.
 */
public class WorldDecode
{
    public static void main(String[] args) throws IOException
    {
        String outDirTest = "/emu/bgda/BG/DATA_extracted/test/test_lmp/";
//        String outDir = "/emu/bgda/BG/DATA_extracted/tavern/tavern_lmp/";
        String outDir = "/emu/bgda/BG/DATA_extracted/cellar1/cellar1_lmp/";

        File outDirFile = new File(outDir);
        outDirFile.mkdirs();

        WorldDecode obj = new WorldDecode();
        obj.read("cellar1.world", outDirFile);
        String txt = obj.disassemble();
        obj.writeFile("cellar1.world.txt", outDirFile, txt);
//        obj.extractTexture("cellar1.world.png", outDirFile);

        outDirFile = new File(outDirTest);
        obj = new WorldDecode();
        obj.read("test.world", outDirFile);
        txt = obj.disassemble();
        obj.writeFile("test.world.txt", outDirFile, txt);

        obj.extractTexture("test.world.png", outDirFile);
    }

    private void writeFile(String filename, File outDirFile, String txt) throws IOException
    {
        File file = new File(outDirFile, filename);
        PrintWriter writer = new PrintWriter(file);
        writer.print(txt);
        writer.close();
    }

    private int fileLength;
    private byte[] fileData;

    private void read(String filename, File outDirFile) throws IOException
    {
        File file = new File(outDirFile, filename);
        BufferedInputStream is = new BufferedInputStream(new FileInputStream(file));

        fileLength = (int) file.length();
        fileData = new byte[fileLength];

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
    }

    private void extractTexture(String outputFilename, File outDirFile) throws IOException
    {
        int offsetTex6c = DataUtil.getLEInt(fileData, 0x6C);
        TexDecode texDecode = new TexDecode();
        texDecode.extract(outDirFile, fileData, offsetTex6c, outputFilename);
    }

    private String disassemble()
    {
        StringBuilder sb = new StringBuilder();

        int numElements = DataUtil.getLEInt(fileData, 0);
        sb.append("Num Elements: ").append(HexUtil.formatHex(numElements)).append("\r\n");


        int offset4 = DataUtil.getLEInt(fileData, 0x4);
        sb.append("Offset4:  ").append(HexUtil.formatHex(offset4)).append("\r\n");

        sb.append("world.08: ").append((DataUtil.getLEInt(fileData, 0x08))).append("\r\n");
        sb.append("world.0C: ").append((DataUtil.getLEInt(fileData, 0x0C))).append("\r\n");

        sb.append("\r\n");

        int cols = DataUtil.getLEInt(fileData,0x10);
        int rows = DataUtil.getLEInt(fileData, 0x14);

        sb.append("Cols: ").append(cols).append("\r\n");
        sb.append("Rows: ").append(rows).append("\r\n");

        int offset18 = DataUtil.getLEInt(fileData, 0x18);
        sb.append("Offset18: ").append(HexUtil.formatHex(offset18)).append("\r\n\r\n");
        // This is an array of 4 byte offsets
        // Each offset points to a -1 terminated array of shorts

        int count1c = DataUtil.getLEInt(fileData, 0x1C);
        sb.append("Count1c: ").append(HexUtil.formatHex(count1c)).append("\r\n");


        int offset20 = DataUtil.getLEInt(fileData, 0x20);
        sb.append("Offset20: ").append(HexUtil.formatHex(offset20)).append("\r\n");

        // Offset 20 points to Count1C elements, each 1c bytes in length
        // member 0x0c in each element is an offset.

        sb.append("\r\n");

        int elementBase = DataUtil.getLEInt(fileData, 0x24);
        sb.append("Element Base: ").append(HexUtil.formatHex(elementBase)).append("\r\n");

        sb.append("\r\n");

        sb.append("world.28: ").append((DataUtil.getLEInt(fileData, 0x28))).append("\r\n");
        sb.append("world.2C: ").append((DataUtil.getLEInt(fileData, 0x2C))).append("\r\n");


        int cols_38 = DataUtil.getLEInt(fileData, 0x30);
        int rows_38 = DataUtil.getLEInt(fileData,0x34);

        sb.append("Cols.38: ").append(cols_38).append("\r\n");
        sb.append("Rows.38: ").append(rows_38).append("\r\n");
        int offset38 = DataUtil.getLEInt(fileData, 0x38);
        sb.append("Offset.38: ").append(HexUtil.formatHex(offset38)).append("\r\n");

        sb.append("world.3C: ").append(DataUtil.getLEInt(fileData, 0x3C)).append("\r\n");
        sb.append("world.40: ").append(DataUtil.getLEInt(fileData, 0x40)).append("\r\n");
        sb.append("world.44: ").append(DataUtil.getLEInt(fileData, 0x44)).append("\r\n");
        sb.append("world.48: ").append(DataUtil.getLEInt(fileData, 0x48)).append("\r\n");

        int offset4c = DataUtil.getLEInt(fileData, 0x4c);
        sb.append("Offset4c: ").append(HexUtil.formatHex(offset4c)).append("\r\n");

        int len50 = DataUtil.getLEInt(fileData, 0x50);
        sb.append("Len50: ").append(HexUtil.formatHex(len50)).append("\r\n");
        int offset54 = DataUtil.getLEInt(fileData, 0x54);
        sb.append("Offset54: ").append(HexUtil.formatHex(offset54)).append("\r\n");

        sb.append("world.58: ").append(DataUtil.getLEInt(fileData, 0x58)).append("\r\n");
        sb.append("world.5c: ").append(DataUtil.getLEInt(fileData, 0x5C)).append("\r\n");
              
        int offset60 = DataUtil.getLEInt(fileData, 0x60);
        sb.append("Offset60: ").append(HexUtil.formatHex(offset60)).append("\r\n");
        int offset64 = DataUtil.getLEInt(fileData, 0x64);
        sb.append("Offset64: ").append(HexUtil.formatHex(offset64)).append("\r\n");

        float offset68 = DataUtil.getLEFloat(fileData, 0x68);
        sb.append("Offset68: ").append(offset68).append("\r\n");

        sb.append("0x44700000 = ").append(Float.intBitsToFloat(0x44700000)).append("\r\n");

        int offsetTex6c = DataUtil.getLEInt(fileData, 0x6C);
        sb.append("Offset Tex 6c: ").append(HexUtil.formatHex(offsetTex6c)).append("\r\n");



        sb.append("-----------------------------------------------------\r\n");
        sb.append("\r\n");
        sb.append("Offsets (18) array. Each index points to an entry in array 20.\r\n \r\n");
        for (int i=0; i<rows*cols; ++i){
            int off = DataUtil.getLEInt(fileData, offset18 + i*4);
            sb.append(i).append(" : ").append(HexUtil.formatHex(off)).append(" -> ");

            int u = DataUtil.getLEShort(fileData, off);
            while (u >= 0){
                sb.append(u);
                off += 2;
                u = DataUtil.getLEShort(fileData, off);
                if (u >= 0){
                    sb.append(", ");
                }
            }

            sb.append("\r\n");
        }

        List<Integer> linkedObjects = new ArrayList<Integer>();

        sb.append("-----------------------------------------------------\r\n");
        sb.append("\r\n");
        sb.append("Elements (20) array - ").append(count1c).append(" elements\r\n \r\n");
        for (int i=0; i<count1c; ++i)
        {
            int off = offset20 + i*0x1c;
            sb.append(i).append(" : ").append(HexUtil.formatHex(off)).append("{\r\n");
            sb.append("    0x00: ").append(DataUtil.getLEShort(fileData, off+0x00)).append("\r\n");
            sb.append("    0x02: ").append(DataUtil.getLEShort(fileData, off+0x02)).append("\r\n");
            sb.append("    0x04: ").append(DataUtil.getLEShort(fileData, off+0x04)).append("\r\n");
            sb.append("    0x06: ").append(DataUtil.getLEShort(fileData, off+0x06)).append("\r\n");
            sb.append("    0x08: ").append(DataUtil.getLEInt(fileData, off+0x08)).append("\r\n");

            // This points to an object - possibly a terrain patch / height map
            // offset 8 and C of it specify rows and cols. 16 bit values start at offset 14.
            // There are rows * cols values.

            int linkedObjectOffset = DataUtil.getLEInt(fileData, off + 0x0c);
            if (!linkedObjects.contains(linkedObjectOffset)){
                linkedObjects.add(linkedObjectOffset);
            }
            sb.append("    0x0c: ").append(HexUtil.formatHex(linkedObjectOffset)).append("\r\n");
            sb.append("    0x10: ").append(DataUtil.getLEShort(fileData, off+0x10)).append("\r\n");
            sb.append("    0x12: ").append(DataUtil.getLEShort(fileData, off+0x12)).append("\r\n");
            sb.append("    0x14: ").append(DataUtil.getLEShort(fileData, off+0x14)).append("\r\n");
            sb.append("    0x16: ").append(DataUtil.getLEShort(fileData, off+0x16)).append("\r\n");
            sb.append("}\r\n");
        }

        sb.append("-----------------------------------------------------\r\n");
        sb.append("\r\n");

        sb.append("Found the following linked objects...").append("\r\n\r\n");
        for (int offset : linkedObjects){
            sb.append("Offset: ").append(HexUtil.formatHex(offset)).append("\r\n");

            sb.append(" 0x00: ").append(DataUtil.getLEInt(fileData, offset)).append("\r\n");
            sb.append(" 0x04: ").append(DataUtil.getLEInt(fileData, offset+4)).append("\r\n");

            int patchWidth = DataUtil.getLEInt(fileData, offset+0x08);
            int patchHeight = DataUtil.getLEInt(fileData, offset+0x0C);

            sb.append(" Dimensions=").append(patchWidth).append(" x ").append(patchHeight).append("\r\n\r\n");

            sb.append(" 0x10: ").append(DataUtil.getLEShort(fileData, offset+0x10)).append("\r\n");
            sb.append(" 0x12: ").append(DataUtil.getLEShort(fileData, offset+0x12)).append("\r\n");
                      
            for (int y=0; y<patchHeight; ++y){
                for (int x=0; x<patchWidth; ++x){
                    int i = y*patchWidth+x;
                    sb.append(DataUtil.getLEShort(fileData, offset+0x14+i*2)).append("\r\n");
                }
                sb.append("--\r\n");
            }
        }

        sb.append("-----------------------------------------------------\r\n");
        sb.append("\r\n");
        sb.append("Elements (24) array - ").append(numElements).append(" elements\r\n \r\n");
        for (int i=0; i<numElements; ++i)
        {
            int off = elementBase + i*0x38;
            sb.append(i).append(" : ").append(HexUtil.formatHex(off)).append("{\r\n");
            sb.append("    0x00: ").append(HexUtil.formatHex(DataUtil.getLEInt(fileData, off))).append("\r\n");
            sb.append("    0x04: ").append(HexUtil.formatHex(DataUtil.getLEInt(fileData, off+4))).append("\r\n");
            sb.append("    0x08: ").append(HexUtil.formatHex(DataUtil.getLEInt(fileData, off+8))).append("\r\n");
            sb.append("    0x0C: ").append(DataUtil.getLEFloat(fileData, off+0x0C)).append("\r\n");
            sb.append("    0x10: ").append(DataUtil.getLEFloat(fileData, off+0x10)).append("\r\n");
            sb.append("    0x14: ").append(DataUtil.getLEFloat(fileData, off+0x14)).append("\r\n");
            sb.append("    0x18: ").append(DataUtil.getLEFloat(fileData, off+0x18)).append("\r\n");
            sb.append("    0x1C: ").append(DataUtil.getLEFloat(fileData, off+0x1C)).append("\r\n");
            sb.append("    0x20: ").append(DataUtil.getLEFloat(fileData, off+0x20)).append("\r\n");
            sb.append("    0x24: ").append(HexUtil.formatHex(DataUtil.getLEInt(fileData, off+0x24))).append("\r\n");
            sb.append("    0x28: ").append(HexUtil.formatHexUShort(DataUtil.getLEShort(fileData, off+0x28))).append("\r\n");
            sb.append("    0x2A: ").append(HexUtil.formatHexUShort(DataUtil.getLEShort(fileData, off+0x2A))).append("\r\n");
            sb.append("    0x2C: ").append(HexUtil.formatHexUShort(DataUtil.getLEShort(fileData, off+0x2C))).append("\r\n");
            sb.append("    0x2E: ").append(HexUtil.formatHexUShort(DataUtil.getLEShort(fileData, off+0x2E))).append("\r\n");
            int member30 = DataUtil.getLEUShort(fileData, off+0x30);
            // test 0x800 for a flag.
            sb.append("    0x30: ").append(HexUtil.formatHexUShort(member30)).append("\r\n");
            sb.append("    0x32: ").append(HexUtil.formatHexUShort(DataUtil.getLEShort(fileData, off+0x32))).append("\r\n");
            sb.append("    0x34: ").append(HexUtil.formatHexUShort(DataUtil.getLEShort(fileData, off+0x34))).append("\r\n");
            sb.append("}\r\n");
        }
        sb.append("-----------------------------------------------------\r\n");
        sb.append("\r\n");
        sb.append("Elements (38) array - ").append(numElements).append(" elements\r\n \r\n");
        // points to an array of 16 bit values, each of which is actually an unsigned byte.
        for (int y = 0; y < rows_38; ++y){
            for (int x=0; x < cols_38; ++x){
                int val = DataUtil.getLEUShort(fileData, offset38 + 2 * (y*cols_38 + x));
                val &= 0xFF;
                sb.append(HexUtil.formatHexByte(val)).append(" ");
            }
            sb.append("\r\n");
        }

        return sb.toString();
    }

}

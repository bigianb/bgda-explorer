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
import java.util.Iterator;
import java.util.List;

/**
 * Decodes an worldname.tex file.
 */
public class LevelTexDecode
{
    public static void main(String[] args) throws IOException
    {
        String inDir="/emu/bgda/BG/DATA/";
        String outDirTest = "/emu/bgda/BG/DATA_extracted/test/test_lmp/";
        String outDir = "/emu/bgda/BG/DATA_extracted/cellar1/cellar1_lmp/";

        File outDirFile = new File(outDir);
        outDirFile.mkdirs();

        File inDirFile = new File(inDir);

        LevelTexDecode obj = new LevelTexDecode();
        obj.read("cellar1.tex", inDirFile);
        String txt;
        txt = obj.disassemble(outDirFile);
        obj.writeFile("cellar1.tex.txt", outDirFile, txt);
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


    private String disassemble(File outDirFile)
    {
        return disassemble(outDirFile, 0x840);
    }

    private String disassemble(File outDirFile, int offset)
    {
        StringBuilder sb = new StringBuilder();

        int header10 =  DataUtil.getLEInt(fileData, offset + 0x10);
        int headerOffset10 = header10 + offset;

        sb.append("Header 10: ").append(HexUtil.formatHex(header10)).append(" (").append(HexUtil.formatHex(headerOffset10)).append(")\r\n");

        int palOffset =  DataUtil.getLEInt(fileData, headerOffset10) + offset;

        sb.append("Pal Offset:  ").append(HexUtil.formatHex(palOffset)).append("\r\n");
        sb.append("Palette:");

        for (int i=0; i < 0x400; i += 4)
        {
            int off = i + palOffset;
            int val = DataUtil.getLEInt(fileData, off);
            if ((i & 0x1f) == 0){
                sb.append("\r\n").append(HexUtil.formatHex(off)).append(": ");
            } else {
                sb.append(", ");
            }
            sb.append(HexUtil.formatHex(val));
        }
        sb.append("\r\n");
        sb.append("\r\nUnknown:");
        for (int i=0; i < 0x800; i += 4)
        {
            int off = i + palOffset + 0x400;
            int val = DataUtil.getLEInt(fileData, off);
            if ((i & 0x1f) == 0){
                sb.append("\r\n").append(HexUtil.formatHex(off)).append(": ");
            } else {
                sb.append(", ");
            }
            sb.append(HexUtil.formatHex(val));
        }
        sb.append("\r\n\r\n");

        int c00 = DataUtil.getLEInt(fileData, palOffset + 0xc00);
        sb.append(HexUtil.formatHex(palOffset + 0xc00)).append(": ").append("pal + 0xc00:  ").append(HexUtil.formatHex(c00)).append("\r\n");
        sb.append("\r\n");

        int c04_offset = palOffset + 0xc04;
        sb.append("pal + 0xc04:  ").append(HexUtil.formatHex(c04_offset));

        for (int i=0; i < c00*2; i += 2)
        {
            int off = i + c04_offset;
            int val = DataUtil.getLEUShort(fileData, off);
            if ((i & 0x0f) == 0){
                sb.append("\r\n").append(HexUtil.formatHex(off)).append(": ");
            } else {
                sb.append(", ");
            }
            sb.append(HexUtil.formatHexUShort(val));
        }

        sb.append("\r\n");
        int bf84 = c04_offset + c00 * 2;
        sb.append("bf84:  ").append(HexUtil.formatHex(bf84)).append("\r\n");

        for (int i=0; i < 0x48; i += 4)
        {
            int off = i + c04_offset + c00 * 2;
            int val = DataUtil.getLEInt(fileData, off);
            if ((i & 0x0f) == 0){
                sb.append("\r\n").append(HexUtil.formatHex(off)).append(": ");
            } else {
                sb.append(", ");
            }
            sb.append(HexUtil.formatHex(val));
        }
        sb.append("\r\n");

        for (int i=0; i < 0x44; i += 4)
        {
            int off = i + c04_offset + c00 * 2 + 0x48;
            int val = DataUtil.getLEInt(fileData, off);
            if ((i & 0x0f) == 0){
                sb.append("\r\n").append(HexUtil.formatHex(off)).append(": ");
            } else {
                sb.append(", ");
            }
            sb.append(HexUtil.formatHex(val));
        }
        sb.append("\r\n").append("\r\n");

        for (int i=0; i < 0x400; i += 2)
        {
            int off = i + c04_offset + c00 * 2 + 0x48 + 0x44;
            int val = DataUtil.getLEUShort(fileData, off);
            if ((i & 0x0f) == 0){
                sb.append("\r\n").append(HexUtil.formatHex(off)).append(": ");
            } else {
                sb.append(", ");
            }
            sb.append(HexUtil.formatHexUShort(val));
        }

        sb.append("\r\n").append("\r\n");
        
        int p = headerOffset10+4;
        while (fileData[p] != -1){
            sb.append(HexUtil.formatHex(p)).append(": x0, y0, x1, y1, p: ").append(fileData[p]).append(", ");
            sb.append(fileData[p+1]).append(", ");
            sb.append(fileData[p+2]).append(", ");
            sb.append(fileData[p+3]).append(", ");

            int poff =  DataUtil.getLEInt(fileData, p+4) + offset;
            sb.append(HexUtil.formatHex(poff)).append("\r\n");

            p += 8;
        }

        sb.append("\r\nDecoded tuples:\r\n");
        Tuple[] tuples = decode(palOffset);
        int i=0;
        for (Tuple tuple : tuples){
            sb.append(i++).append(": ").append(tuple.a).append(", ").append(tuple.b).append("\r\n");
        }

        return sb.toString();
    }

    class Tuple
    {
        public short a;
        public short b;
    }

    public Tuple[] decode(int palOffset)
    {
        Tuple[] out = new Tuple[256];

        int len = DataUtil.getLEInt(fileData, palOffset+0xc00);
        int bitValOffset = palOffset + 0x0c04 + 2 * len + 0x48;
        int lookupOffset = palOffset + 0x0c04 + 2 * len;


        for (int i=0; i<256; ++i){
            int bit=1;
            int a = i >> (8-bit);
            int v = DataUtil.getLEInt(fileData, bitValOffset + bit*4);
            if (v < a){
                do {
                    ++bit;
                    if (bit > 8){
                        break;
                    }
                    a = i >> (8-bit);
                    v = DataUtil.getLEInt(fileData, bitValOffset + bit*4);
                } while (v < a);
            }
            out[i] = new Tuple();
            if (bit <= 8){
                int val = DataUtil.getLEInt(fileData, lookupOffset + bit*4);
                int c04Index = a + val;
                out[i].a = DataUtil.getLEShort(fileData, palOffset + 0x0c04 + c04Index * 2 );
                out[i].b = (short)bit;
            }
        }

        return out;
    }

}

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
        sb.append("\r\n");

        int p = headerOffset10+4;
        while (fileData[p] != -1){
            sb.append("x0: ").append(fileData[p]).append("\r\n");
            sb.append("y0: ").append(fileData[p+1]).append("\r\n");
            sb.append("x1: ").append(fileData[p+2]).append("\r\n");
            sb.append("y1: ").append(fileData[p+3]).append("\r\n");

            int poff =  DataUtil.getLEInt(fileData, p+4) + offset;
            sb.append("p: ").append(HexUtil.formatHex(poff)).append("\r\n\r\n");

            p += 8;
        }
        return sb.toString();
    }

}

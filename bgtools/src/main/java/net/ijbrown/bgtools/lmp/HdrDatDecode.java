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

import javax.imageio.ImageIO;
import java.awt.image.BufferedImage;
import java.io.*;

/**
 * Decodes a *.HDR / *.DAT file pair
 */
public class HdrDatDecode
{
    public static void main(String[] args) throws IOException
    {
        String inDir = "/emu/bgda/RTA/BG/DATA/";

        String outDir = "/emu/bgda/RTA/BG/DATA_extracted/";

        File outDirFile = new File(outDir);
        outDirFile.mkdirs();

        File inDirFile = new File(inDir);

        HdrDatDecode obj = new HdrDatDecode();
        obj.read("EQCACHE", inDirFile);
        String txt;
        txt = obj.disassemble();
        obj.writeFile("eqcache_hdr.txt", outDirFile, txt);
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

    public void read(String filename, File dir) throws IOException
    {
        File file = new File(dir, filename+".HDR");
        read(file);
    }

    public void read(File file) throws IOException
    {
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

    private String disassemble()
    {
        StringBuilder sb = new StringBuilder();

        int numEntries = DataUtil.getLEInt(fileData,  0);

        sb.append("Num entries: ").append(numEntries).append("\r\n");

        int offset=4;
        for (int i=0; i<numEntries; ++i){
           int elOffset = DataUtil.getLEInt(fileData, offset);
           int stringOffset = DataUtil.getLEInt(fileData, offset+4);
           int numEls = DataUtil.getLEInt(fileData, offset+8);

           String name = DataUtil.collectString(fileData, stringOffset);

           sb.append("\r\n");
           sb.append("Name: ").append(name).append("\r\n");
           sb.append("numEls: ").append(numEls).append("\r\n");
           sb.append("Els: ").append("\r\n");
           for (int el=0; el < numEls; ++el){
               short s1 = DataUtil.getLEShort(fileData, elOffset); elOffset += 2;
               short s2 = DataUtil.getLEShort(fileData, elOffset); elOffset += 2;
               short s3 = DataUtil.getLEShort(fileData, elOffset); elOffset += 2;
               short s4 = DataUtil.getLEShort(fileData, elOffset); elOffset += 2;
               sb.append(s1).append(", ").append(s2).append(", ").append(s3).append(", ").append(s4).append("\r\n");
           }
           offset += 12;
        }

        return sb.toString();
    }

}

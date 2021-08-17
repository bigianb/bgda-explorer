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

/**
 * Decodes a *.HDR / *.DAT file pair
 */
public class HdrDatDecode
{
    public static void main(String[] args) throws IOException
    {
        /*
        String inDir = "/emu/bgda/RTA/BG/DATA/";
        String outDir = "/emu/bgda/RTA/BG/DATA_extracted/";
*/
        String inDir = "/Users/ian/ps2_games/CON/BG/DATA/";
        String outDir = "/Users/ian/ps2_games/CON/BG/DATA_extracted/";

        File outDirFile = new File(outDir);
        outDirFile.mkdirs();

        File inDirFile = new File(inDir);

        HdrDatDecode obj = new HdrDatDecode();
        obj.read("EQCACHE", inDirFile);
        String txt;
        txt = obj.disassemble();
        obj.writeFile("eqcache_hdr.txt", outDirFile, txt);

        obj.extractFiles(inDirFile, outDirFile);
    }

    private void extractFiles(File dir, File outDirFile)throws IOException
    {
        File datFile = new File(dir, baseFilename+".DAT");
        var entry = headerEntries[0];
        for (int el=0; el<2; ++el){
            var element = entry.headerElements[el];
            var elData = readData(datFile, element.startPosBytes, element.lenBytes);


            var elBaseName = baseFilename+"_"+entry.name+element.id+"_"+el;

            File outFile = new File(outDirFile, elBaseName + ".dat");
            BufferedOutputStream os = new BufferedOutputStream(new FileOutputStream(outFile));
            os.write(elData, 0, element.lenBytes);
            os.close();

            // elements come in pairs, the first looks like a texture
            if ((el & 1) == 0){
                new TexDecode().extract(outDirFile, elData, 0, elBaseName, elData.length);
            }

        }
    }

    private void writeFile(String filename, File outDirFile, String txt) throws IOException
    {
        File file = new File(outDirFile, filename);
        PrintWriter writer = new PrintWriter(file);
        writer.print(txt);
        writer.close();
    }

    static class HeaderEntry
    {
        String name;
        HeaderElement[] headerElements;
    }

    private HeaderEntry[] headerEntries;

    static class HeaderElement
    {
        int id;
        int lenBytes;
        int startPosBytes;
    }

    private byte[] headerFileData;
    private String baseFilename;

    private byte[] readData(File datFile, int start, int len) throws IOException
    {
        BufferedInputStream is = new BufferedInputStream(new FileInputStream(datFile));

        var data = new byte[len];

        var skipped = is.skip(start);
        if (start != skipped){
            throw new IOException("wrong number of bytes skipped");
        }
        is.readNBytes(data, 0, len);

        is.close();
        return data;
    }

    public void read(String filename, File dir) throws IOException
    {
        baseFilename = filename;
        File file = new File(dir, filename+".HDR");
        readHeader(file);
        parseFileData();
    }

    public void readHeader(File file) throws IOException
    {
        BufferedInputStream is = new BufferedInputStream(new FileInputStream(file));

        int headerFileLength = (int) file.length();
        headerFileData = new byte[headerFileLength];

        int offset = 0;
        int remaining = headerFileLength;
        while (remaining > 0) {
            int read = is.read(headerFileData, offset, remaining);
            if (read == -1) {
                throw new IOException("Read less bytes then expected when reading file");
            }
            remaining -= read;
            offset += read;
        }
    }

    private void parseFileData()
    {
        int numEntries = DataUtil.getLEInt(headerFileData,  0);
        headerEntries = new HeaderEntry[numEntries];

        int offset=4;
        for (int i=0; i<numEntries; ++i) {
            int elOffset = DataUtil.getLEInt(headerFileData, offset);
            int stringOffset = DataUtil.getLEInt(headerFileData, offset + 4);
            int numEls = DataUtil.getLEInt(headerFileData, offset + 8);
            String name = DataUtil.collectString(headerFileData, stringOffset);

            var entry = new HeaderEntry();
            entry.name = name;
            entry.headerElements = new HeaderElement[numEls];
            for (int el=0; el < numEls; ++el){
                var element = new HeaderElement();
                element.id = DataUtil.getLEShort(headerFileData, elOffset); elOffset += 2;
                element.lenBytes = 2048 * DataUtil.getLEShort(headerFileData, elOffset); elOffset += 2;
                element.startPosBytes = 2048 * DataUtil.getLEInt(headerFileData, elOffset); elOffset += 4;
                entry.headerElements[el] = element;
            }
            headerEntries[i] = entry;
            offset += 12;
        }
    }


    private String disassemble()
    {
        StringBuilder sb = new StringBuilder();

        sb.append("Num entries: ").append(headerEntries.length).append("\r\n");

        int offset=4;
        for (int i=0; i<headerEntries.length; ++i){
            var entry = headerEntries[i];
            sb.append("\r\n");
            sb.append("Name: ").append(entry.name).append("\r\n");
            sb.append("numEls: ").append(entry.headerElements.length).append("\r\n");
            sb.append("Els: ").append("\r\n");
            for (int el=0; el < entry.headerElements.length; ++el){
                var element = entry.headerElements[el];
                sb.append("ID: ").append(element.id).append(", Len: ").append(element.lenBytes).append(", Start: ").append(element.startPosBytes).append("\r\n");
            }
            offset += 12;
        }

        return sb.toString();
    }

}

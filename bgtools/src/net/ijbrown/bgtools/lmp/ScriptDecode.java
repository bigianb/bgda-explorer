/*  Copyright (C) 2012 Ian Brown

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
 * Decodes a script file.
 */
public class ScriptDecode
{
    public static void main(String[] args) throws IOException
    {
        String rootDir = "/emu/bgda/BG/DATA_extracted/";
        String lmpName = "tavern";

        String outDir = rootDir + lmpName + "/" + lmpName + "_lmp/";

        File outDirFile = new File(outDir);
        outDirFile.mkdirs();

        ScriptDecode obj = new ScriptDecode();
        obj.read("script.scr", outDirFile);
        String txt;
        txt = obj.disassemble(outDirFile);
        obj.writeFile("script.scr.txt", outDirFile, txt);
    }

    private void writeFile(String filename, File outDirFile, String txt) throws IOException
    {
        File file = new File(outDirFile, filename);
        try (PrintWriter writer = new PrintWriter(file)) {
            writer.print(txt);
        }
    }

    private byte[] fileData;

    private void read(String filename, File outDirFile) throws IOException
    {
        File file = new File(outDirFile, filename);
        BufferedInputStream is = new BufferedInputStream(new FileInputStream(file));

        int fileLength = (int) file.length();
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

    // There is a 0x60 byte header followed by the body.
    private final int bodyOffset = 0x60;

    private String disassemble(File outDirFile)
    {
        StringBuilder sb = new StringBuilder();


        int offset0 = DataUtil.getLEInt(fileData, bodyOffset);
        int hw1 = DataUtil.getLEUShort(fileData, bodyOffset + 0x04);
        int hw2 = DataUtil.getLEUShort(fileData, bodyOffset + 0x06);
        int hw3 = DataUtil.getLEUShort(fileData, bodyOffset + 0x08);
        int hw4 = DataUtil.getLEUShort(fileData, bodyOffset + 0x0A);

        int instructionsOffset = DataUtil.getLEInt(fileData, bodyOffset + 0x0C);
        int stringsOffset = DataUtil.getLEInt(fileData, bodyOffset + 0x10);
        int offset3 = DataUtil.getLEInt(fileData, bodyOffset + 0x14);
        int offset4 = DataUtil.getLEInt(fileData, bodyOffset + 0x18);
        int offset5 = DataUtil.getLEInt(fileData, bodyOffset + 0x1C);

        int numInternals = DataUtil.getLEUShort(fileData, bodyOffset + 0x20);
        int offsetInternals = DataUtil.getLEInt(fileData, bodyOffset + 0x24);

        int numExternals = DataUtil.getLEUShort(fileData, bodyOffset + 0x28);
        int offsetExternals = DataUtil.getLEInt(fileData, bodyOffset + 0x2C);

        sb.append(numInternals).append(" Internals:\n");
        sb.append("~~~~~~~~~~~~\n");
        for (int i = 0; i < numInternals; ++i) {
            printInternal(sb, offsetInternals + 0x18 * i);
        }
        sb.append("\n");

        sb.append(numExternals).append(" Externals:\n");
        sb.append("~~~~~~~~~~~~\n");
        for (int i = 0; i < numExternals; ++i) {
            printExternal(sb, offsetExternals + 0x18 * i);
        }
        sb.append("\n");

        sb.append("Instructions\n");
        sb.append("~~~~~~~~~~~~\n\n");
        dumpInstructions(sb, instructionsOffset, stringsOffset - instructionsOffset);
        sb.append("\n");

        sb.append("Strings\n");
        sb.append("~~~~~~~\n\n");
        dumpStrings(sb, stringsOffset, offset3 - stringsOffset);
        sb.append("\n");
        return sb.toString();
    }

    private void dumpStrings(StringBuilder sb, int stringsOffset, int len)
    {
        boolean needsOffset = true;
        for (int i = 0; i < len; i += 4) {
            if (needsOffset) {
                sb.append(HexUtil.formatHex(stringsOffset + i)).append(": ");
                needsOffset = false;
            }
            int ival = DataUtil.getLEInt(fileData, stringsOffset + i + bodyOffset);
            for (int b = 3; b >= 0; --b) {
                int c = (ival >> (b * 8)) & 0xff;
                if (0 == c) {
                    sb.append("\n");
                    needsOffset = true;
                    break;
                }
                sb.append((char) c);
            }

        }
    }

    private void dumpInstructions(StringBuilder sb, int instructionsOffset, int len)
    {
        for (int i = 0; i < len; i += 4) {
            int opcode = DataUtil.getLEInt(fileData, instructionsOffset + i + bodyOffset);
            sb.append(HexUtil.formatHex(opcode)).append("\n");
        }
    }

    private void printInternal(StringBuilder sb, int offset)
    {
        sb.append(HexUtil.formatHexUShort(offset)).append(": ");
        int id = DataUtil.getLEInt(fileData, offset + bodyOffset);
        sb.append(HexUtil.formatHex(id)).append(" - ");
        sb.append(DataUtil.collectString(fileData, offset + bodyOffset + 4));
        sb.append("\n");
    }

    private void printExternal(StringBuilder sb, int offset)
    {
        sb.append(HexUtil.formatHexUShort(offset)).append(": ");
        sb.append(DataUtil.collectString(fileData, offset + bodyOffset + 4));
        sb.append("\n");
    }
}

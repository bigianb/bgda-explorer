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
 * Decodes an Objects.ob file.
 */
public class ObjectsDecode
{
    public static void main(String[] args) throws IOException
    {
        GameType gameType = GameType.JUSTICE_LEAGUE_HEROES;

        Config config = new Config(gameType);
        String inDir = config.getDataDir();
        String outDir = inDir+"../DATA_extracted/";

        decode(outDir+"intro/intro_lmp/");
    }

    private static void decode(String dirName) throws IOException
    {
        File outDirFile = new File(dirName);

        ObjectsDecode obj = new ObjectsDecode();
        obj.read("objects.ob", outDirFile);
        String txt = obj.disassemble();
        obj.writeFile("objects.ob.txt", outDirFile, txt);
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

    private String disassemble()
    {
        StringBuilder sb = new StringBuilder();
        int numObjs = DataUtil.getLEShort(fileData, 0);
        int flags = DataUtil.getLEUShort(fileData, 2);
        int stringOffset = DataUtil.getLEInt(fileData, 4);

        sb.append("Flags: ").append(flags);
        sb.append("\r\n");
        sb.append("\r\n");
        int objOffset = 8;
        // object data starts at offset 8
        for (int objNum = 0; objNum < numObjs; ++objNum) {
            sb.append("Object ").append(objNum).append("\r\n");

            // If >=0, this is the index into the string table.
            int strIdx = DataUtil.getLEInt(fileData, objOffset);
            String name = Integer.toString(strIdx);
            if (strIdx >= 0) {
                name = DataUtil.collectString(fileData, stringOffset + strIdx);
            }
            sb.append("    name: ").append(name).append("\r\n");

            int objLen = DataUtil.getLEUShort(fileData, objOffset + 4);
            sb.append("    Len: ").append(HexUtil.formatHexUShort(objLen)).append("\r\n");

            int i6 = DataUtil.getLEUShort(fileData, objOffset + 6);
            sb.append("    i6: ").append(HexUtil.formatHexUShort(i6)).append("\r\n");
            int rot = i6 >> 12;
            // 4 = 90 deg
            double drot = 22.5 * rot;
            sb.append("        rot = ").append(drot).append("\r\n");

            float f1 = DataUtil.getLEFloat(fileData, objOffset + 8);
            float f2 = DataUtil.getLEFloat(fileData, objOffset + 12);
            float f3 = DataUtil.getLEFloat(fileData, objOffset + 16);

            sb.append("    Floats: ").append(f1).append(", ").append(f2).append(", ").append(f3).append("\r\n");

            int lenSoFar = 20;
            while (lenSoFar < objLen) {
                int i = DataUtil.getLEInt(fileData, objOffset + lenSoFar);
                if (i > 0) {
                    sb.append("    prop: ").append(DataUtil.collectString(fileData, stringOffset + i)).append("\r\n");
                }
                lenSoFar += 4;
            }
            sb.append("\r\n");
            objOffset += objLen;
        }

        sb.append("String Table\r\n\r\n");
        int off = stringOffset;
        while (off < fileLength) {
            sb.append(off - stringOffset).append(": '");
            String s = "";
            while (fileData[off] != 0) {
                s += (char) fileData[off];
                ++off;
            }
            sb.append(s).append("'\r\n");
            ++off;
        }

        return sb.toString();
    }

}

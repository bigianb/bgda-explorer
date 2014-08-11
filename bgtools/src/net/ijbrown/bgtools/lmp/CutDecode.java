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
 * Decodes a .cut file
 */
public class CutDecode
{
    public static void main(String[] args) throws IOException
    {
        decodeLmp("cuttown", "intro.cut");
    }

    private static void decodeLmp(String lmpName, String cutName) throws IOException
    {
        String outDir = "/emu/bgda/BG/DATA_extracted/" + lmpName + "/" + lmpName + "_lmp/";
        File outDirFile = new File(outDir);

        CutDecode obj = new CutDecode();
        obj.read(cutName, outDirFile);
        String txt = obj.disassemble();
        obj.writeFile(cutName+".txt", outDirFile, txt);
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
        int characterBlockOffset = DataUtil.getLEInt(fileData, 0x0C);
        int numCharacters = DataUtil.getLEInt(fileData, 0x10);

        sb.append("Character Block offset: ").append(HexUtil.formatHex(characterBlockOffset)).append("\r\n");
        sb.append("Num Characters: ").append(numCharacters).append("\r\n");

        return sb.toString();
    }

}

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
        int flags = DataUtil.getLEInt(fileData, 0);
        int keyframeOffset = DataUtil.getLEInt(fileData, 4);
        int numKeyframes = DataUtil.getLEInt(fileData, 8);
        int characterBlockOffset = DataUtil.getLEInt(fileData, 0x0C);
        int numCharacters = DataUtil.getLEInt(fileData, 0x10);

        String string20 = DataUtil.collectString(fileData, 0x20);
        String string48 = DataUtil.collectString(fileData, 0x48);
        String string74 = DataUtil.collectString(fileData, 0x74);

        sb.append("Flags: ").append(HexUtil.formatHex(flags)).append("\r\n");
        sb.append("Keyframe offset: ").append(HexUtil.formatHex(keyframeOffset)).append("\r\n");
        sb.append("num keyframes: ").append(HexUtil.formatHex(numKeyframes)).append("\r\n");
        sb.append("Character Block offset: ").append(HexUtil.formatHex(characterBlockOffset)).append("\r\n");
        sb.append("Num Characters: ").append(numCharacters).append("\r\n");
        sb.append("String 20: '").append(string20).append("'\r\n");
        sb.append("String 48: '").append(string48).append("'\r\n");
        sb.append("String 74: '").append(string74).append("'\r\n");

        sb.append("\r\nCharacters:\r\n\r\n");

        for (int i=0; i<numCharacters; ++i)
        {
            int charOffset = characterBlockOffset + i * 0x2C;
            String charName = DataUtil.collectString(fileData, charOffset + 8);
            sb.append("Name: '").append(charName).append("'\r\n");
            float x = DataUtil.getLEFloat(fileData, charOffset + 0x1C);
            float y = DataUtil.getLEFloat(fileData, charOffset + 0x20);
            float z = DataUtil.getLEFloat(fileData, charOffset + 0x24);
            sb.append("  Pos: ").append(x).append(", ").append(y).append(", ").append(z).append("\r\n");
            int extraData = DataUtil.getLEInt(fileData, charOffset + 4);
            sb.append("  extraData: ").append(HexUtil.formatHex(extraData)).append("\r\n");
            int shortVal = DataUtil.getLEUShort(fileData, charOffset + 0x28);
            sb.append("  shortVal: ").append(HexUtil.formatHexUShort(shortVal)).append("\r\n");
            int rot = shortVal >> 12;
            // 4 = 90 deg
            double drot = 22.5 * rot;
            sb.append("    rot = ").append(drot).append("\r\n");
            sb.append("\r\n");
        }

        sb.append("\r\nKey Frames:\r\n\r\n");

        for (int frame=0; frame<numKeyframes; ++frame)
        {
            int frameOffset = keyframeOffset + frame * 0x14;
            float time = DataUtil.getLEFloat(fileData, frameOffset);
            sb.append("Frame ").append(frame).append(": t=").append(time);
            int h4 = DataUtil.getLEShort(fileData, frameOffset+4);
            int h6 = DataUtil.getLEShort(fileData, frameOffset+6);
            sb.append(", h4=").append(HexUtil.formatHexUShort(h4));
            sb.append(", h6=").append(HexUtil.formatHexUShort(h6));
            int x = DataUtil.getLEInt(fileData, frameOffset + 0x8);
            int y = DataUtil.getLEInt(fileData, frameOffset + 0xC);
            int z = DataUtil.getLEInt(fileData, frameOffset + 0x10);
            sb.append(", Pos: ").append(x).append(", ").append(y).append(", ").append(z);
            sb.append("\r\n");
        }

        return sb.toString();
    }

}

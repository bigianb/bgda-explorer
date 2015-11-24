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
import java.util.HashMap;
import java.util.Map;

/**
 * Decodes a Font.
 */
public class FntDecode
{
    public static void main(String[] args) throws IOException
    {
        String outDir = "D:\\emu\\bgda\\BG\\DATA_extracted\\frontend";

        File outDirFile = new File(outDir);

        String fontname = "gamefontsmall.fnt";
        FntDecode obj = new FntDecode();
        String txt = obj.disassemble(fontname, outDirFile);
        obj.writeFile(fontname + ".txt", outDirFile, txt);

        fontname = "bigfont.fnt";
        obj = new FntDecode();
        txt = obj.disassemble(fontname, outDirFile);
        obj.writeFile(fontname + ".txt", outDirFile, txt);
    }

    private void writeFile(String filename, File outDirFile, String txt) throws IOException
    {
        File file = new File(outDirFile, filename);
        PrintWriter writer = new PrintWriter(file);
        writer.print(txt);
        writer.close();
    }

    public String disassemble(String filename, File outDirFile) throws IOException
    {
        File file = new File(outDirFile, filename);
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

        TexDecode texDecode = new TexDecode();
        int textureOffset = DataUtil.getLEInt(fileData, 0x10);
        int textureLength = fileLength - textureOffset;
        texDecode.extract(outDirFile, fileData, textureOffset, filename, textureLength);

        return disassemble(fileData, 0);
    }

    /*
        struct font
        {
                short num_glyphs;
                short pad;
                int pad2;
                glyph* pGlyph;
                p* p1;
                byte* tex;
        }

        struct glyph
        {
                unsigned short charId;
                unsigned short pad;
                int pad[3];
        }

     */

    public String disassemble(byte[] fileData, int startOffset) throws IOException
    {
        StringBuilder sb = new StringBuilder(2048);

        int numGlyphs = DataUtil.getLEShort(fileData, startOffset);
        int glyphTableOffset = DataUtil.getLEInt(fileData, startOffset + 8);
        int unknownOffset = DataUtil.getLEInt(fileData, startOffset + 0x0C);
        int textureOffset = DataUtil.getLEInt(fileData, startOffset + 0x10);

        sb.append("Number of glyphs:   ").append(numGlyphs).append("\n");
        sb.append("Glyph table offset: ").append(HexUtil.formatHex(glyphTableOffset)).append("\n");
        sb.append("kern pair offset:   ").append(HexUtil.formatHex(unknownOffset)).append("\n");
        sb.append("Offset to texture:  ").append(HexUtil.formatHex(textureOffset)).append("\n");
        sb.append("\nGlyph Table\n\n");

        Map<Integer, Integer> glyphToCharMap = disassembleGlyphTable(fileData, startOffset+glyphTableOffset, numGlyphs, sb);
        disassemblKernPairTable(fileData, startOffset + unknownOffset, sb, glyphToCharMap);

        return sb.toString();
    }

    private Map<Integer, Integer> disassembleGlyphTable(byte[] fileData, int startOffset, int numGlyphs, StringBuilder sb)
    {
        Map<Integer, Integer> glyphToCharMap = new HashMap<>(256);
        for (int glyphNum=0; glyphNum < numGlyphs; ++glyphNum){
            int glyphOffset = startOffset + glyphNum * 0x10;
            int charId = DataUtil.getLEShort(fileData, glyphOffset);
            sb.append("Glyph ").append(HexUtil.formatHexShort(glyphNum)).append(",  charId=").append(HexUtil.formatHexShort(charId));
            char c = (char)charId;
            sb.append("  '").append(c).append("'\n");
            sb.append("            ").append(HexUtil.formatHexShort(DataUtil.getLEShort(fileData, glyphOffset + 0x02))).append("\n");
            sb.append("            ").append(HexUtil.formatHexShort(DataUtil.getLEShort(fileData, glyphOffset + 0x04))).append("\n");
            sb.append("            ").append(HexUtil.formatHexShort(DataUtil.getLEShort(fileData, glyphOffset + 0x06))).append("\n");
            sb.append("            ").append(HexUtil.formatHexShort(DataUtil.getLEShort(fileData, glyphOffset + 0x08))).append("\n");
            sb.append("            ").append(HexUtil.formatHexShort(DataUtil.getLEShort(fileData, glyphOffset + 0x0A))).append("\n");
            sb.append("    width = ").append(HexUtil.formatHexShort(DataUtil.getLEShort(fileData, glyphOffset + 0x0C))).append("\n");
            sb.append("  kern id = ").append(HexUtil.formatHexShort(DataUtil.getLEShort(fileData, glyphOffset + 0x0E))).append("\n");
            sb.append("\n");
            glyphToCharMap.put(glyphNum, charId);
        }
        return glyphToCharMap;
    }

    private void disassemblKernPairTable(byte[] fileData, int startOffset, StringBuilder sb, Map<Integer, Integer> glyphToCharMap)
    {
        sb.append("\nKern Pair Table\n\n");
        for (int glyphNum=0; glyphNum < 1000; ++glyphNum){
            int glyphOffset = startOffset + glyphNum * 6;
            int glyph1 = DataUtil.getLEShort(fileData, glyphOffset);
            if (glyph1 < 0){
                break;
            }
            int glyph2 = DataUtil.getLEShort(fileData, glyphOffset + 0x02);
            int char1 = glyphToCharMap.get(glyph1);
            int char2 = glyphToCharMap.get(glyph2);
            sb.append("Entry ").append(HexUtil.formatHexShort(glyphNum)).append("   '").append((char) char1).append("' '").append((char)char2).append("'\n");
            sb.append("  glyph1=").append(HexUtil.formatHexShort(glyph1));
            sb.append(", glyph2=").append(HexUtil.formatHexShort(glyph2));
            sb.append(", kern=  ").append(HexUtil.formatHexShort(DataUtil.getLEShort(fileData, glyphOffset + 0x04))).append("\n");
            sb.append("\n");

        }
    }

}

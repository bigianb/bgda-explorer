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
import java.io.BufferedInputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;

/**
 * Decodes a Texture.
 */
public class TexDecode
{
    public static void main(String[] args) throws IOException
    {
        GameType gameType = GameType.DARK_ALLIANCE;

        Config config = new Config(gameType);
        String inDir = config.getDataDir();
        String outDir = inDir+"../DATA_extracted/";

        TexDecode obj = new TexDecode();
        obj.extract("lowbartender.tex", new File(outDir + "tavern/bartend_lmp"));
        obj.extract("bartender.tex", new File(outDir + "tavern/bartend_lmp"));

    }

    // Texture format is something like as follows:
    // 16 byte header.
    //    short width
    //    short height

    // Then starting offset stored at address 0x10
    // GS Packet

    public void extract(String filename, File outDirFile) throws IOException
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

        extract(outDirFile, fileData, 0, filename, fileLength);
    }

    private static final int BITBLTBUF = 0x50;
    private static final int TRXPOS = 0x51;
    private static final int TRXREG = 0x52;

    private static final int PSMCT32 = 0x00;
    private static final int PSMT4 = 0x14;

    public void extract(File outDirFile, byte[] fileData, int startOffset, String filename, int length) throws IOException
    {
        GSMemory gsMem = new GSMemory();

        int endIndex = startOffset + length;
        int finalw = DataUtil.getLEShort(fileData, startOffset);
        int finalh = DataUtil.getLEShort(fileData, startOffset + 2);

        int sourcew = finalw;
        int sourceh = finalh;

        PalEntry[] pixels = null;
        byte[] bytes = null;

        int offsetToGIF = DataUtil.getLEInt(fileData, startOffset + 16);

        if (length == 0){
            int dataLen = DataUtil.getLEShort(fileData, startOffset+0x06) * 16;
            endIndex = startOffset + offsetToGIF + dataLen;
        }

        int curIdx = offsetToGIF + startOffset;
        GIFTag gifTag = new GIFTag();
        gifTag.parse(fileData, curIdx);

        // This is basically heuristics
        if (gifTag.nloop == 4) {
            // should look for TRXREG really
            int palw = DataUtil.getLEShort(fileData, curIdx + 0x30);
            int palh = DataUtil.getLEShort(fileData, curIdx + 0x34);

            curIdx += gifTag.getLength();
            GIFTag gifTag2 = new GIFTag();
            gifTag2.parse(fileData, curIdx);

            // 8 bit palletised
            PalEntry[] palette = PalEntry.readPalette(fileData, curIdx + 0x10, palw, palh);

            palette = PalEntry.unswizzlePalette(palette);

            curIdx += gifTag2.getLength();

            int destWBytes = (finalw + 0x0f) & ~0x0f;
            int destHBytes = (finalh + 0x0f) & ~0x0f;

            int dpsm = PSMCT32;
            int dbw = 0;
            int dbp = 0;
            int rrw = 0;
            int rrh = 0;
            int startx = 0;
            int starty = 0;

            while (curIdx < endIndex - 0x10) {
                GIFTag gifTag3 = new GIFTag();
                gifTag3.parse(fileData, curIdx);
                while (!gifTag3.isImage()){
                    int trxregOffset = findADEntry(fileData, curIdx + 0x10, gifTag3.nloop, TRXREG);
                    if (trxregOffset != 0) {
                        rrw = DataUtil.getLEShort(fileData, trxregOffset);
                        rrh = DataUtil.getLEShort(fileData, trxregOffset + 4);
                    }
                    int trxposOffset = findADEntry(fileData, curIdx + 0x10, gifTag3.nloop, TRXPOS);
                    if (trxposOffset != 0) {
                        startx = DataUtil.getLEShort(fileData, trxposOffset + 0x04) & 0x07FF;
                        starty = DataUtil.getLEShort(fileData, trxposOffset + 0x06) & 0x07FF;
                    }
                    int bitbltOffset = findADEntry(fileData, curIdx + 0x10, gifTag3.nloop, BITBLTBUF);
                    if (bitbltOffset != 0){
                        //int sbw = fileData[bitbltOffset + 0x02] & 0x3F;
                        dbp = fileData[bitbltOffset + 0x04] & 0x3FFF;
                        dbw = fileData[bitbltOffset + 0x06] & 0x3F;
                        dpsm = fileData[bitbltOffset + 0x07] & 0x3F;
                    }

                    curIdx += gifTag3.getLength();
                    gifTag3.parse(fileData, curIdx);
                }

                curIdx += 0x10;     // image gif tag
                int bytesToTransfer = gifTag3.nloop * 16;
                if (palette.length == 16){
                    // source is PSMT4. Dest can be PSMT4 or PSMCT32
                    if (dpsm == PSMCT32) {
                        byte[] imageData = fileData;
                        int imageDataIdx = curIdx;
                        // check for multiple IMAGE entries.
                        int nextTagInd = bytesToTransfer + curIdx;
                        if (nextTagInd < endIndex - 0x10){
                            GIFTag imageTag2 = new GIFTag();
                            imageTag2.parse(fileData, nextTagInd);
                            if (imageTag2.flg == 2){
                                // IMAGE
                                int bytesToTransfer2 = imageTag2.nloop * 16;
                                imageDataIdx = 0;
                                imageData = new byte[bytesToTransfer+bytesToTransfer2];
                                int j = curIdx;
                                for (int i=0; i< bytesToTransfer; ++i){
                                    imageData[i] = fileData[j];
                                }
                                j = nextTagInd + 0x10;
                                for (int i=bytesToTransfer; i< bytesToTransfer+bytesToTransfer2; ++i){
                                    imageData[i] = fileData[j];
                                }
                                bytesToTransfer += imageTag2.getLength();
                            }
                        }

                        gsMem.writeTexPSMCT32(dbp, dbw, startx, starty, rrw, rrh, imageData, imageDataIdx);

                        destWBytes = (finalw + 0x3f) & ~0x3f;
                        bytes = gsMem.readTexPSMT4(dbp, destWBytes / 0x40, startx, starty, destWBytes, destHBytes);
                        bytes = expand4bit(bytes);

                    } else {
                        // dest and source are the same and so image isn't swizzled
                        bytes = transferPSMT4(bytes, fileData, curIdx, startx, starty, rrw, rrh, destWBytes, destHBytes);
                    }
                } else {
                    // source is PSMT8. Dest is always PSMCT32.
                    gsMem.writeTexPSMCT32(dbp, dbw, startx, starty, rrw, rrh, fileData, curIdx);
                }
                curIdx += bytesToTransfer;
            }
            if (palette.length == 256){
                destWBytes = (finalw + 0x3f) & ~0x3f;
                dbw = destWBytes / 0x40;
                bytes = gsMem.readTexPSMT8(dbp, dbw, 0, 0, destWBytes, finalh);
            }
            pixels = applyPalette(palette, bytes);
            sourcew = destWBytes;
            sourceh = destHBytes;

        } else if (gifTag.nloop == 3) {
            GIFTag gifTag2 = new GIFTag();
            gifTag2.parse(fileData, startOffset + 0xC0);
            System.out.println(gifTag2.toString());

            if (gifTag2.flg == 2) {
                // image mode
                pixels = readPixels32(fileData, startOffset + 0xD0, finalw, finalh);
            }
        }
        if (finalw != 0 && pixels != null) {
            BufferedImage image = new BufferedImage(finalw, finalh, BufferedImage.TYPE_INT_ARGB);
            for (int y = 0; y < sourceh && y < finalh; ++y) {
                for (int x = 0; x < sourcew && x < finalw; ++x) {
                    PalEntry pixel = pixels[y * sourcew + x];
                    if (pixel != null) {
                        image.setRGB(x, y, pixel.argb());
                    }
                }
            }
            File outputfile = new File(outDirFile, filename+".png");
            ImageIO.write(image, "png", outputfile);
        }
    }

    // Take an image where the pixels are packed and expand them to one byte per pixel.
    private byte[] expand4bit(byte[] bytes){
        byte[] out = new byte[bytes.length *2];
        int j=0;
        for (int val : bytes) {
            out[j++] = (byte) (val & 0x0f);
            out[j++] = (byte) ((val >> 4) & 0x0f);
        }
        return out;
    }

    private PalEntry[] applyPalette(PalEntry[] palette, byte[] bytes)
    {
        PalEntry[] pixels = new PalEntry[bytes.length];
        for (int i=0; i<bytes.length; ++i){
            pixels[i] = palette[bytes[i] & 0xFF];
        }
        return pixels;
    }

    private int findADEntry(byte[] fileData, int dataStartIdx, int nloop, int registerId)
    {
        int retval = 0;
        for (int i=0; i<nloop; ++i){
            int reg = DataUtil.getLEInt(fileData, dataStartIdx + i * 0x10 + 0x08);
            if (reg == registerId){
                retval = dataStartIdx + i*0x10;
                break;
            }
        }
        return retval;
    }

    private byte[] transferPSMT4(byte[] pixels, byte[] fileData, int startOffset, int startx, int starty,
                                 int rrw, int rrh, int destWBytes, int destHBytes)
    {
        if (pixels == null) {
            pixels = new byte[destWBytes * destHBytes];
        }

        int nybble=2;
        byte[] nybbles = new byte[2];
        int idx = startOffset;
        for (int y = 0; y < rrh && (y+starty) < destHBytes; ++y) {
            for (int x = 0; x < rrw; ++x) {
                if (nybble > 1){
                    byte twoPix = fileData[idx++];
                    nybbles[0] = (byte)((twoPix) & 0x0f);
                    nybbles[1] = (byte)((twoPix >> 4) & 0x0f);
                    nybble = 0;
                }
                int destIdx = (y+starty) * destWBytes + (x + startx);
                pixels[destIdx] = nybbles[nybble];
                ++nybble;
            }
        }
        return pixels;
    }

    private PalEntry[] readPixels32(byte[] fileData, int startOffset, int w, int h)
    {
        int numPixels = w * h;
        PalEntry[] pixels = new PalEntry[numPixels];
        int destIdx = 0;
        int endOffset = startOffset + numPixels * 4;
        for (int idx = startOffset; idx < endOffset; ) {
            PalEntry pe = new PalEntry();
            pe.r = fileData[idx++];
            pe.g = fileData[idx++];
            pe.b = fileData[idx++];
            pe.a = fileData[idx++];

            pixels[destIdx++] = pe;
        }

        return pixels;
    }



}

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
        obj.extract("bartender.tex", new File(outDir + "tavern/bartend_lmp"));

    }

    // Texture format is something like as follows:
    // 16 byte header.
    //    short width
    //    short height

    // Then starting offset stored at address 0x10
    // GS Packet

    // Currently it is assumed that all the image data is in one GIFTag segment.
    // This is not the case for some textures (e.g. chest_large), so the code will
    // need extending to read the GIFTags properly.

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
            endIndex = offsetToGIF + dataLen;
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

            while (curIdx < endIndex - 0x10) {
                GIFTag gifTag3 = new GIFTag();

                int trxregOffset = 0;
                while (trxregOffset == 0 && curIdx < endIndex - 0x10) {
                    gifTag3.parse(fileData, curIdx);
                    trxregOffset = findADEntry(fileData, curIdx + 0x10, gifTag3.nloop, TRXREG);
                    if (trxregOffset == 0) {
                        curIdx += gifTag3.getLength();
                    }
                }

                int rrw = DataUtil.getLEShort(fileData, trxregOffset);
                int rrh = DataUtil.getLEShort(fileData, trxregOffset + 4);

                int startx = 0;
                int starty = 0;
                int trxposOffset = findADEntry(fileData, curIdx + 0x10, gifTag3.nloop, TRXPOS);
                if (trxposOffset != 0) {
                    startx = DataUtil.getLEShort(fileData, trxposOffset + 0x04) & 0x07FF;
                    starty = DataUtil.getLEShort(fileData, trxposOffset + 0x06) & 0x07FF;
                }

                int bitbltOffset = findADEntry(fileData, curIdx + 0x10, gifTag3.nloop, BITBLTBUF);
                if (bitbltOffset != 0){
                    dbw = fileData[bitbltOffset + 0x06];
                    dpsm = fileData[bitbltOffset + 0x07];
                }

                curIdx += gifTag3.getLength();
                GIFTag imageTag = new GIFTag();
                imageTag.parse(fileData, curIdx);
                curIdx += 0x10;     // image gif tag
                int bytesToTransfer = imageTag.nloop * 16;
                int pixelsSize = rrw*rrh;
                if (palette.length == 16){
                    destWBytes = 0x300;
                    destHBytes = pixelsSize*8 / destWBytes;
                    // source is PSMT4. Dest can be PSMT4 or PSMCT32
                    if (dpsm == PSMCT32) {
                        // source data is PSMT4 but transferred as though it was PSMCT32 .. so it will be swizzled
                        int xferw = rrw*4;          // Each dest pixel is 4 bytes
                        //int startpix = startx*4;
                        int pix1 = rrw*rrh*8;
                        int pix2 = destHBytes * destWBytes;
                        if (pix1 == pix2){
                            bytes = transferData(bytes, fileData, curIdx, startx, starty, destWBytes, destHBytes, destWBytes, destHBytes);
                            //bytes = transferPSMT4(bytes, fileData, curIdx, startx, starty, destWBytes, destHBytes, destWBytes, destHBytes, dbw);

                        } else {
                            throw new RuntimeException("confused in texture decode");
                        }
                        bytes = transferPSMT4(bytes, fileData, curIdx, startx, starty, destWBytes, destHBytes, destWBytes, destHBytes, dbw);
                    } else {
                        // dest and source are the same and so image isn't swizzled
                        bytes = transferPSMT4(bytes, fileData, curIdx, startx, starty, rrw, rrh, destWBytes, destHBytes, dbw);
                    }
                } else {
                    // source is PSMT8. Dest is always PSMCT32.
                    int xferw = rrw*4;          // Each dest pixel is 4 bytes
                    int startpix = startx*4;
                    bytes = transferData(bytes, fileData, curIdx, startpix, starty, xferw, rrh, destWBytes, destHBytes);
                }

                curIdx += bytesToTransfer;
            }
            if (palette.length == 256){
                bytes = unswizzle8bpp(bytes, destWBytes, destHBytes);
            } else {
                if (dpsm == PSMCT32) {
                    bytes = unswizzle4bpp(bytes, destWBytes, destHBytes);
                } else {
                    // no swizzle required
                }
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

    private byte[] unswizzle8bpp(byte[] pixels, int w, int h)
    {
        // See pp 174 of GS Users manual.
        // Converting one column PSMT8 (16x4) to PSMCT32 (8x2)
        byte[] unswizzled = new byte[pixels.length];

        for (int y = 0; y < h; ++y) {
            for (int x = 0; x < w; ++x) {

                int block_location = (y & (~0xf)) * w + (x & (~0xf)) * 2;
                int swap_selector = (((y + 2) >> 2) & 0x1) * 4;
                int posY = (((y & (~3)) >> 1) + (y & 1)) & 0x7;
                int column_location = posY * w * 2 + ((x + swap_selector) & 0x7) * 4;

                int byte_num = ((y >> 1) & 1) + ((x >> 2) & 2);     // 0,1,2,3

                int idx = block_location + column_location + byte_num;
                if (idx >= pixels.length) {
                    System.out.println("x");
                } else {
                    unswizzled[(y * w) + x] = pixels[idx];
                }
            }
        }

        return unswizzled;
    }

    private byte[] unswizzle4bpp(byte[] pixels, int w, int h)
    {
        byte[] unswizzled = new byte[pixels.length];

        for (int y = 0; y < h; ++y) {
            for (int x = 0; x < w; ++x) {

                int pageX = x &(~0x7f);
                int pageY = y &(~0x7f);

                int pages_horz = (w + 0x7f) >> 7;
                int pages_vert = (h + 0x7f) >> 7;

                int page_number = (pageY >> 7) * pages_horz + (pageX >> 7);

                int page32Y = (page_number / pages_vert) * 32;
                int page32X = (page_number % pages_vert) * 64;

                int page_location = page32Y * h * 2 + page32X * 4;

                int locX = x & 0x7f;
                int locY = y & 0x7f;

                int block_location = ((locX & (~0x1f)) >> 1) * h + (locY & (~0xf)) * 2;
                int swap_selector = (((y + 2) >> 2) & 0x1) * 4;
                int posY = (((y & (~3)) >> 1) + (y & 1)) & 0x7;

                int column_location = posY * h * 2 + ((x + swap_selector) & 0x7) * 4;

                int byte_num = (x >> 3) & 3;     // 0,1,2,3

                int idx = page_location + block_location + column_location + byte_num;
                if (idx >= pixels.length) {
                    System.out.println("x");
                } else {
                    byte entry = pixels[idx];

                    entry = (byte)((entry >> (((y >> 1) & 0x01) * 4)) & 0x0F);

                    unswizzled[(y * w) + x] = entry;
                }
            }
        }

        return unswizzled;
    }

    private byte[] transferPSMT4(byte[] pixels, byte[] fileData, int startOffset, int startx, int starty,
                                 int rrw, int rrh, int destWBytes, int destHBytes, int dbw)
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
                    nybbles[0] = (byte)((twoPix >> 4) & 0x0f);
                    nybbles[1] = (byte)((twoPix) & 0x0f);
                    nybble = 0;
                }
                int destIdx = (y+starty) * destWBytes + (x + startx);
                pixels[destIdx++] = nybbles[nybble];
                ++nybble;
            }
        }
        return pixels;
    }

    private byte[] transferData(byte[] pixels, byte[] fileData, int startOffset, int startx, int starty,int rrw, int rrh, int destWBytes, int destHBytes)
    {
        if (pixels == null) {
            int numDestBytes = destWBytes * destHBytes;
            pixels = new byte[numDestBytes];
        }
        int interleave = 2;
        if (rrh*2 > destHBytes){
            interleave = 1; // hack
        }
        int idx = startOffset;
        for (int y = 0; y < rrh && (y+starty) < destHBytes; ++y) {
            for (int x = 0; x < rrw; ++x) {
                int destIdx = (y+starty) * destWBytes * interleave + (x + startx);
                pixels[destIdx++] = fileData[idx++];
            }
        }
        return pixels;
    }

    private PalEntry[] readPixels32(PalEntry[] pixels, byte[] fileData, PalEntry[] palette, int startOffset, int startx, int starty, int rrw, int rrh, int dbw, int dbh)
    {
        // rrw and rrh are the size in pixels assuming RGBA transmission mode.
        if (palette.length == 256){
            int numDestBytes = dbh * dbw * 4;
            int widthBytes = dbw * 4;
            if (pixels == null) {
                pixels = new PalEntry[numDestBytes];
            }
            int idx = startOffset;
            for (int y = 0; y < rrh && (y+starty) < dbh; ++y) {
                for (int x = 0; x < rrw; ++x) {
                    int destIdx = (y+starty) * widthBytes + (x + startx) * 4;
                    pixels[destIdx++] = palette[fileData[idx++] & 0xFF];
                    pixels[destIdx++] = palette[fileData[idx++] & 0xFF];
                    pixels[destIdx++] = palette[fileData[idx++] & 0xFF];
                    pixels[destIdx] = palette[fileData[idx++] & 0xFF];
                }
            }
            return pixels;
        } else {
            // dbw and dbh are the source w and height in 16 bit words rounded up to a 16 byte boundary.
            int numDestBytes = dbh * dbw * 4;
            int widthBytes = dbw*2;
            if (pixels == null) {
                pixels = new PalEntry[numDestBytes];
            }
            int idx = startOffset;
            boolean lowbit=false;
            for (int y = 0; y < rrh; ++y) {
                for (int x = 0; x < rrw; ++x) {
                    int destIdx = (y + starty) * widthBytes + (x + startx);
                    if (lowbit){
                        pixels[destIdx] = palette[fileData[idx] >> 4 & 0x0F];
                        idx++;
                    } else {
                        pixels[destIdx] = palette[fileData[idx] & 0x0F];
                    }
                    lowbit = !lowbit;
                }
            }
            return pixels;
        }
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

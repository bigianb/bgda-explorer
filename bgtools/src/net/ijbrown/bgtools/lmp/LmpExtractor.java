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
 * extracts files from a .lmp file.
 */
public class LmpExtractor {
    
    public static void main(String[] args) throws IOException
    {
        String filename = "/emu/bgda/BG/DATA/pend.lmp";

        String outDir = "/emu/bgda/BG/DATA_extracted/pend/";

        File outDirFile = new File(outDir);
        outDirFile.mkdirs();

        LmpExtractor obj = new LmpExtractor();
        obj.extractAll(filename, outDirFile);
    }

    private void extractAll(String filename, File outDir) throws IOException
    {
        File file = new File(filename);
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
        extractAll(fileData, 0, outDir);
    }

    public void extractAll(byte[] fileData, int fileStartOffset, File outDir) throws IOException
    {
        int numFiles = DataUtil.getLEInt(fileData, fileStartOffset);
        System.out.println("LMP contains " + numFiles + " Files.");

        for (int fileNo=0; fileNo < numFiles; ++fileNo){
            int headerOffset = fileStartOffset + 4 + fileNo * 0x40;
            String subfileName = DataUtil.collectString(fileData, headerOffset);

            int subOffset = DataUtil.getLEInt(fileData, headerOffset + 0x38);
            int subLen = DataUtil.getLEInt(fileData, headerOffset + 0x3C);

            System.out.println("Extracting: " + subfileName + ", offset=" + subOffset + ", length=" + subLen);

            File outFile = new File(outDir, subfileName);
            BufferedOutputStream os = new BufferedOutputStream(new FileOutputStream(outFile));
            os.write(fileData, fileStartOffset + subOffset, subLen);
            os.close();
        }

    }


}

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

    private final GameType gameType;

    public LmpExtractor(GameType gameType) {
        this.gameType=gameType;
    }

    public static void main(String[] args) throws IOException
    {
        GameType gameType = GameType.JUSTICE_LEAGUE_HEROES;

        Config config = new Config(gameType);
        String inDir = config.getDataDir();
        String outDir = inDir+"../DATA_extracted/";

        LmpExtractor obj = new LmpExtractor(gameType);
        obj.extractAll(inDir, outDir, "SUPERMAN.LMP");
    }

    private void extractAll(String inDirname, String outDirname, String lmpFilename) throws IOException
    {
        File outDir = new File(outDirname+lmpFilename.replace('.', '_'));
        outDir.mkdirs();

        File file = new File(inDirname+lmpFilename);
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

        int headerOffset = fileStartOffset+4;
        for (int fileNo=0; fileNo < numFiles; ++fileNo){
            int stringOffset=0;
            int subOffset=0;
            int subLen=0;
            String subfileName;
            if (gameType == GameType.DARK_ALLIANCE) {
                // Name inline with header
                subfileName = DataUtil.collectString(fileData, headerOffset);
                subOffset = DataUtil.getLEInt(fileData, headerOffset + 0x38);
                subLen = DataUtil.getLEInt(fileData, headerOffset + 0x3C);
                headerOffset += 0x40;
            } else {
                // name referenced from header
                stringOffset = DataUtil.getLEInt(fileData, headerOffset);
                subOffset = DataUtil.getLEInt(fileData, headerOffset+4);
                subLen = DataUtil.getLEInt(fileData, headerOffset+8);
                subfileName = DataUtil.collectString(fileData, fileStartOffset+stringOffset);
                headerOffset += 0x0C;
            }
            System.out.println("Extracting: " + subfileName + ", offset=" + subOffset + ", length=" + subLen);

            File outFile = new File(outDir, subfileName);
            BufferedOutputStream os = new BufferedOutputStream(new FileOutputStream(outFile));
            os.write(fileData, fileStartOffset + subOffset, subLen);
            os.close();
        }

    }


}

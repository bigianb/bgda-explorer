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
 * Decodes a GOB file.
 */
public class GobExtractor
{
    private final GameType gameType;

    public GobExtractor(GameType gameType)
    {
        this.gameType=gameType;
    }

    public static void main(String[] args) throws IOException
    {
        GameType gameType = GameType.JUSTICE_LEAGUE_HEROES;

        Config config = new Config(gameType);

        String inDir = config.getDataDir();

        String outDir = inDir+"../DATA_extracted/";

        File outDirFile = new File(outDir);
        outDirFile.mkdirs();

        GobExtractor obj = new GobExtractor(gameType);
        obj.extract("intro", outDirFile, new File(inDir));
        obj.extract("E1L1A", outDirFile, new File(inDir));
        /*
        obj.extract("cellar1", outDirFile, new File(inDir));
        obj.extract("cuttown", outDirFile, new File(inDir));
        obj.extract("smlcave1", outDirFile, new File(inDir));
        obj.extract("tavern", outDirFile, new File(inDir));
        obj.extract("test", outDirFile, new File(inDir));
        obj.extract("town", outDirFile, new File(inDir));
        obj.extract("burneye1", outDirFile, new File(inDir));
        obj.extract("cuttown", outDirFile, new File(inDir));
        */

    }

    private void extract(String name, File outRoot, File inDir) throws IOException
    {
        File file = new File(inDir, name + ".gob");
        BufferedInputStream is = new BufferedInputStream(new FileInputStream(file));

        File outDir = new File(outRoot, name);
        outDir.mkdirs();

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

        offset=0;
        String lmpName = DataUtil.collectString(fileData, offset);
        LmpExtractor lmpExtractor = new LmpExtractor(gameType);
        while (!lmpName.isEmpty()){
            System.out.println("Extracting " + lmpName + " from " + file.getName());
            File lmpOutputDir = new File(outDir, lmpName.replace('.', '_'));
            lmpOutputDir.mkdir();
            int lmpDataOffset = DataUtil.getLEInt(fileData, offset + 0x20);
            lmpExtractor.extractAll(fileData, lmpDataOffset, lmpOutputDir);

            offset += 0x28;
            lmpName = DataUtil.collectString(fileData, offset);
        }
    }

}

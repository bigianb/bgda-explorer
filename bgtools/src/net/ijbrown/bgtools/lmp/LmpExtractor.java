package net.ijbrown.bgtools.lmp;

import java.io.*;

/**
 * extracts files from a .lmp file.
 */
public class LmpExtractor {
    
    public static void main(String[] args) throws IOException
    {
        String filename = "/emu/bgda/BG/DATA/snowflag.lmp";

        String outDir = "/emu/bgda/BG/DATA_extracted/";

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

        int numFiles = DataUtil.getLEInt(fileData, 0);
        System.out.println("LMP contains " + numFiles + " Files.");

        for (int fileNo=0; fileNo < numFiles; ++fileNo){
            int headerOffset = 4 + fileNo * 0x40;
            String subfileName = DataUtil.collectString(fileData, headerOffset);

            int subOffset = DataUtil.getLEInt(fileData, headerOffset + 0x38);
            int subLen = DataUtil.getLEInt(fileData, headerOffset + 0x3C);

            System.out.println("Extracting: " + subfileName + ", offset=" + subOffset + ", length=" + subLen);

            File outFile = new File(outDir, subfileName);
            BufferedOutputStream os = new BufferedOutputStream(new FileOutputStream(outFile));
            os.write(fileData, subOffset, subLen);
            os.close();
        }

    }



}

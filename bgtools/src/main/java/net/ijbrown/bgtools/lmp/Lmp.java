package net.ijbrown.bgtools.lmp;

import java.io.*;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.HashMap;
import java.util.Map;

public class Lmp {

    private byte[] fileData = null;
    private int fileStartOffset = 0;
    private final GameType gameType;

    public Lmp(GameType gameType)
    {
        this.gameType = gameType;
    }

    public void readLmpFile(Path path) throws IOException {
        fileData = Files.readAllBytes(path);
        fileStartOffset = 0;
        readDirectory();
    }

    private void readDirectory()
    {
        int numFiles = DataUtil.getLEInt(fileData, fileStartOffset);
        int headerOffset = fileStartOffset+4;
        for (int fileNo=0; fileNo < numFiles; ++fileNo) {
            int stringOffset = 0;
            int subOffset = 0;
            int subLen = 0;
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
                subOffset = DataUtil.getLEInt(fileData, headerOffset + 4);
                subLen = DataUtil.getLEInt(fileData, headerOffset + 8);
                subfileName = DataUtil.collectString(fileData, fileStartOffset + stringOffset);
                headerOffset += 0x0C;
            }
            Entry entry = new Entry(subOffset + fileStartOffset, subLen, subfileName, fileData);
            directory.put(subfileName, entry);
        }
    }

    public Entry findEntry(String name)
    {
        return directory.get(name);
    }


    private final Map<String, Entry> directory = new HashMap<>();

    public static class Entry
    {
        public String name;
        public int offset;
        public int length;
        public byte[] data;

        public Entry(int offset, int length, String name, byte[] data) {
            this.offset = offset;
            this.length = length;
            this.name = name;
            this.data = data;
        }
    }
}

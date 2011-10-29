package net.ijbrown.bgtools.lmp;

/**
 * Utilities for reading binary data.
 */
public class DataUtil
{
    public static String collectString(byte[] fileData, int headerOffset)
    {
        String s = "";
        int i=headerOffset;
        while (fileData[i] != 0){
            s += (char)fileData[i];
            ++i;
        }
        return s;
    }

    public static int getLEInt(byte[] data, int offset) {
        return data[offset+3] << 24 | (data[offset + 2] & 0xff) << 16 | (data[offset + 1] & 0xff) << 8 | (data[offset] & 0xff);
    }

    public static short getLEShort(byte[] data, int offset) {
        return (short)((data[offset+1] & 0xff) << 8 | (data[offset] & 0xff));
    }
}

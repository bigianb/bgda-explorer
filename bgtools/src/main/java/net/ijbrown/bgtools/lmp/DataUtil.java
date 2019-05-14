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

    public static float getLEFloat(byte[] data, int offset) {
        int i = getLEInt(data, offset);
        return Float.intBitsToFloat(i);
    }

    public static int getLEInt(byte[] data, int offset) {
        return data[offset+3] << 24 | (data[offset + 2] & 0xff) << 16 | (data[offset + 1] & 0xff) << 8 | (data[offset] & 0xff);
    }

    public static short getLEShort(byte[] data, int offset) {
        return (short)((data[offset+1] & 0xff) << 8 | (data[offset] & 0xff));
    }

    public static int getLEUShort(byte[] data, int offset) {
        return ((data[offset+1] & 0xff) << 8 | (data[offset] & 0xff));
    }
}

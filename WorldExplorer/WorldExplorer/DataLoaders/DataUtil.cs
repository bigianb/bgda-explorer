/*  Copyright (C) 2012 Ian Brown

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

using System;
using System.Text;

namespace WorldExplorer.DataLoaders
{
    class DataUtil
    {
        public static string GetString(byte[] data, int index)
        {
            StringBuilder sb = new StringBuilder();
            int i = index;

            // TODO: Check array length before access
            while (data[i] != 0) {
                sb.Append((char)data[i]);
                ++i;
            }
            return sb.ToString();
        }

        public static bool FilePathHasInvalidChars(string path)
        {
            return (!string.IsNullOrEmpty(path) && path.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0);
        }

        public static int getLEInt(byte[] data, int offset)
        {
            return BitConverter.ToInt32(data, offset);
        }

        public static short getLEShort(byte[] data, int offset)
        {
            return BitConverter.ToInt16(data, offset);
        }

        public static float getLEFloat(byte[] data, int offset)
        {
            return BitConverter.ToSingle(data, offset);
        }

        public static ushort getLEUShort(byte[] data, int offset)
        {
            return BitConverter.ToUInt16(data, offset);
        }

    }
}

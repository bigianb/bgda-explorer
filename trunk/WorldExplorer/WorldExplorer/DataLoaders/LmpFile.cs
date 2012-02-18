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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorldExplorer.DataLoaders
{
    public class LmpFile
    {
        public LmpFile(string name, byte[] data, int startOffset, int dataLen)
        {
            Name = name;
            FileData = data;
            _startOffset = startOffset;
            _dataLen = dataLen;
        }

        public String Name;

        public void ReadDirectory()
        {
            int numEntries = BitConverter.ToInt32(FileData, _startOffset);
            for (int entry = 0; entry < numEntries; ++entry) {
                int headerOffset = _startOffset + 4 + entry * 0x40;
                String subfileName = DataUtil.GetString(FileData, headerOffset);

                int subOffset = BitConverter.ToInt32(FileData, headerOffset + 0x38);
                int subLen = BitConverter.ToInt32(FileData, headerOffset + 0x3C);

                var info = new EntryInfo() { Name = subfileName, StartOffset = subOffset + _startOffset, Length=subLen };
                Directory[subfileName] = info;
            }
        }

        private int _startOffset;
        private int _dataLen;
        public byte[] FileData;

        public class EntryInfo
        {
            public string Name;
            public int StartOffset;
            public int Length;
        }

        public Dictionary<String, EntryInfo> Directory = new Dictionary<string, EntryInfo>();
    }
}

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
            var reader = new DataReader(FileData, _startOffset, _dataLen);
            int numEntries = reader.ReadInt32(); //BitConverter.ToInt32(FileData, _startOffset);

            // Detect if it's a new version or not
            bool isNewVersion = false;
            string firstFileName = DataUtil.GetString(FileData, _startOffset + 4);

            // If file name has invalid characers or doesn't have an extension assume it's not actually a file name
            if (DataUtil.FilePathHasInvalidChars(firstFileName) || string.IsNullOrEmpty(Path.GetExtension(firstFileName)))
                isNewVersion = true;


            for (int entry = 0; entry < numEntries; ++entry) {
                if (isNewVersion)
                {
                    // Champions RTA version
                    int stringOffset = reader.ReadInt32();
                    int dataOffset = reader.ReadInt32();
                    int dataLength = reader.ReadInt32();

                    var tempOffset = reader.Offset;
                    reader.SetOffset(stringOffset);
                    var name = reader.ReadZString();
                    reader.SetOffset(tempOffset);

                    var info = new EntryInfo() { Name = name, StartOffset = dataOffset + _startOffset, Length = dataLength };
                    Directory[name] = info;
                }
                else
                {
                    // BA:DA 1 version
                    int headerOffset = _startOffset + 4 + entry * 64;
                    String subfileName = DataUtil.GetString(FileData, headerOffset);

                    int subOffset = BitConverter.ToInt32(FileData, headerOffset + 56);
                    int subLen = BitConverter.ToInt32(FileData, headerOffset + 60);

                    var info = new EntryInfo()
                    {Name = subfileName, StartOffset = subOffset + _startOffset, Length = subLen};
                    Directory[subfileName] = info;
                }
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

        public EntryInfo FindFirstEntryWithSuffix(String suffix)
        {
            EntryInfo entry = Directory.Where(x => x.Key.EndsWith(suffix)).FirstOrDefault().Value;
            return entry;
        }
    }
}

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
using System.Collections.Generic;
using System.Linq;

namespace WorldExplorer.DataLoaders
{
    public class LmpFile
    {
        public LmpFile(EngineVersion engineVersion, string name, byte[] data, int startOffset, int dataLen)
        {
            _engineVersion = engineVersion;
            Name = name;
            FileData = data;
            _startOffset = startOffset;
            _dataLen = dataLen;
        }

        private readonly EngineVersion _engineVersion;
        /// <summary>
        /// The .lmp file name.
        /// </summary>
        public string Name;

        public void ReadDirectory()
        {
            var reader = new DataReader(FileData, _startOffset, _dataLen);
            int numEntries = reader.ReadInt32();

            for (int entry = 0; entry < numEntries; ++entry) {
                if (EngineVersion.ReturnToArms == _engineVersion || EngineVersion.JusticeLeagueHeroes == _engineVersion)
                {
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
                    int headerOffset = _startOffset + 4 + entry * 64;
                    string subfileName = DataUtil.GetString(FileData, headerOffset);

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
        /// <summary>
        /// The raw data of the .lmp file.
        /// </summary>
        public byte[] FileData;

        public class EntryInfo
        {
            public string Name;
            public int StartOffset;
            public int Length;
        }

        /// <summary>
        /// A directory of embeded files where the file names are the keys.
        /// </summary>
        public Dictionary<string, EntryInfo> Directory = new Dictionary<string, EntryInfo>();

        public EntryInfo FindFirstEntryWithSuffix(string suffix)
        {
            EntryInfo entry = Directory.Where(x => x.Key.EndsWith(suffix)).FirstOrDefault().Value;
            return entry;
        }
        public EntryInfo FindFile(string file)
        {
            foreach (var ent in Directory)
            {
                if (string.Compare(ent.Key, file, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return ent.Value;
            }
            return null;
        }
    }
}

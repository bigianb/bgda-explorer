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

using JetBlackEngineLib;
using JetBlackEngineLib.Io;
using System;
using System.Collections.Generic;

namespace WorldExplorer.DataLoaders
{
    public class LmpFile
    {
        private readonly int _dataLen;
        private readonly EngineVersion _engineVersion;

        private readonly int _startOffset;

        /// <summary>
        /// A directory of embedded files where the file names are the keys.
        /// </summary>
        public readonly Dictionary<string, EntryInfo> Directory = new();

        /// <summary>
        /// The raw data of the .lmp file.
        /// </summary>
        public readonly byte[] FileData;

        /// <summary>
        /// The .lmp file name.
        /// </summary>
        public readonly string Name;

        public LmpFile(EngineVersion engineVersion, string name, byte[] data, int startOffset, int dataLen)
        {
            _engineVersion = engineVersion;
            Name = name;
            FileData = data;
            _startOffset = startOffset;
            _dataLen = dataLen;
        }

        public void ReadDirectory()
        {
            DataReader reader = new(FileData, _startOffset, _dataLen);
            var numEntries = reader.ReadInt32();

            for (var entry = 0; entry < numEntries; ++entry)
            {
                if (_engineVersion is EngineVersion.ReturnToArms or EngineVersion.JusticeLeagueHeroes)
                {
                    var stringOffset = reader.ReadInt32();
                    var dataOffset = reader.ReadInt32();
                    var dataLength = reader.ReadInt32();

                    var tempOffset = reader.Offset;
                    reader.SetOffset(stringOffset);
                    var name = reader.ReadZString();
                    reader.SetOffset(tempOffset);
                    
                    Directory[name] = new(name, dataOffset + _startOffset, dataLength);
                }
                else
                {
                    var headerOffset = _startOffset + 4 + (entry * 64);
                    var subFileName = DataUtil.GetString(FileData, headerOffset);

                    var subOffset = BitConverter.ToInt32(FileData, headerOffset + 56);
                    var subLen = BitConverter.ToInt32(FileData, headerOffset + 60);
                    
                    Directory[subFileName] = new(subFileName, subOffset + _startOffset, subLen);
                }
            }
        }

        public EntryInfo? FindFirstEntryWithSuffix(string suffix)
        {
            foreach (var (key, value) in Directory)
            {
                if (key.EndsWith(suffix)) return value;
            }
            return null;
        }

        public EntryInfo? FindFile(string file)
        {
            foreach (var ent in Directory)
            {
                if (string.Compare(ent.Key, file, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    return ent.Value;
                }
            }

            return null;
        }

        public class EntryInfo
        {
            public readonly int Length;
            public string Name;
            public readonly int StartOffset;

            public EntryInfo(string name, int startOffset, int length)
            {
                Length = length;
                Name = name;
                StartOffset = startOffset;
            }
        }
    }
}
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
using System.Collections.Generic;

namespace WorldExplorer.DataLoaders
{
    public class YakFile
    {
        public EngineVersion EngineVersion { get; }

        public readonly List<YakEntry> Entries = new();

        /// <summary>
        /// The raw data of the .yak file.
        /// </summary>
        public readonly byte[] FileData;

        /// <summary>
        /// The .yak file name.
        /// </summary>
        public readonly string Name;

        public YakFile(EngineVersion engineVersion, string name, byte[] data)
        {
            EngineVersion = engineVersion;
            Name = name;
            FileData = data;
        }

        public void ReadEntries()
        {
            Entries.Clear();
            DataReader reader = new(FileData);
            YakEntry? entry;
            while ((entry = ReadEntry(reader)) != null)
            {
                Entries.Add(entry);
            }
        }

        private static YakEntry? ReadEntry(DataReader reader)
        {
            var child1 = ReadChild(reader);
            return child1 == null 
                ? null 
                : new YakEntry(new [] {child1, ReadChild(reader), ReadChild(reader), ReadChild(reader)});
        }

        private static YakEntryChild? ReadChild(DataReader reader)
        {
            var t = reader.ReadInt32();
            if (t == 0)
            {
                return null;
            }

            return new() {TextureOffset = t, VifOffset = reader.ReadInt32(), VifLength = reader.ReadInt32()};
        }

        public class YakEntryChild
        {
            public int TextureOffset;
            public int VifLength;
            public int VifOffset;
        }

        public class YakEntry
        {
            public readonly YakEntryChild?[] Children;

            public YakEntry(YakEntryChild?[] children)
            {
                Children = children;
            }
        }
    }
}
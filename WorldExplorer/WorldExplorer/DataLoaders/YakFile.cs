﻿/*  Copyright (C) 2012 Ian Brown

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

using System.Collections.Generic;

namespace WorldExplorer.DataLoaders
{
    public class YakFile
    {
        public YakFile(EngineVersion engineVersion, string name, byte[] data)
        {
            _engineVersion = engineVersion;
            Name = name;
            FileData = data;
        }

        private readonly EngineVersion _engineVersion;

        /// <summary>
        /// The .yak file name.
        /// </summary>
        public string Name;

        /// <summary>
        /// The raw data of the .yak file.
        /// </summary>
        public byte[] FileData;

        public class Child
        {
            public int TextureOffset;
            public int VifOffset;
            public int VifLength;
        }

        public class Entry
        {
            public Child[] children = new Child[4];
        }

        public List<Entry> Entries = new List<Entry>();

        public void ReadEntries()
        {
            Entries.Clear();
            var reader = new DataReader(FileData);
            Entry entry;
            while ((entry = readEntry(reader)) != null)
            {
                Entries.Add(entry);
            }
        }

        Entry readEntry(DataReader reader)
        {
            var child1 = readChild(reader);
            if (child1 == null)
            {
                return null;
            }
            var entry = new Entry();
            entry.children[0] = child1;
            entry.children[1] = readChild(reader);
            entry.children[2] = readChild(reader);
            entry.children[3] = readChild(reader);
            return entry;
        }

        Child readChild(DataReader reader)
        {
            var t = reader.ReadInt32();
            if (t == 0)
            {
                return null;
            }
            var child = new Child
            {
                TextureOffset = t,
                VifOffset = reader.ReadInt32(),
                VifLength = reader.ReadInt32()
            };
            return child;
        }

    }
}

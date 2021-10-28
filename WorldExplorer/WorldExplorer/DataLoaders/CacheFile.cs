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

namespace WorldExplorer.DataLoaders
{
    public class CacheFile
    {
        private readonly EngineVersion _engineVersion;

        public List<Entry> Entries = new();

        /// <summary>
        /// The raw data of the .dat file.
        /// </summary>
        public byte[] FileData;

        /// <summary>
        /// The raw data of the .hdr file.
        /// </summary>
        public byte[] HeaderData;

        /// <summary>
        /// The .hdr file name.
        /// </summary>
        public string Name;

        public CacheFile(EngineVersion engineVersion, string name, byte[] hdrData, byte[] datData)
        {
            _engineVersion = engineVersion;
            Name = name;
            HeaderData = hdrData;
            FileData = datData;
        }

        public void ReadEntries()
        {
            Entries.Clear();
            DataReader reader = new(HeaderData);
            var numEntries = reader.ReadInt32();
            for (var entryNo = 0; entryNo < numEntries; ++entryNo)
            {
                Entries.Add(readEntry(reader));
            }
        }

        private Entry readEntry(DataReader reader)
        {
            var childrenOffset = reader.ReadInt32();
            var nameOffset = reader.ReadInt32();
            var numChildren = reader.ReadInt32();

            var nextElOffset = reader.Offset;

            reader.Offset = nameOffset;
            var name = reader.ReadZString();

            Entry entry = new(name);
            reader.Offset = childrenOffset;
            for (var i = 0; i < numChildren; i += 2)
            {
                entry.children.Add(readChild(reader));
            }

            reader.Offset = nextElOffset;
            return entry;
        }

        private Child readChild(DataReader reader)
        {
            var id = reader.ReadInt16();
            var len = 2048 * reader.ReadInt16();
            var start = 2048 * reader.ReadInt32();

            var id2 = reader.ReadInt16();
            var len2 = 2048 * reader.ReadInt16();
            var start2 = 2048 * reader.ReadInt32();

            if (id != id2)
            {
                throw new Exception("expected ids to match");
            }

            Child child = new();
            child.id = id;
            child.TexLength = len;
            child.TexOffset = start;
            child.VifLength = len2;
            child.VifOffset = start2;

            return child;
        }

        public class Child
        {
            public int id;
            public int TexLength;
            public int TexOffset;
            public int VifLength;
            public int VifOffset;
        }

        public class Entry
        {
            public List<Child> children = new();
            public string name;

            public Entry(string name)
            {
                this.name = name;
            }
        }
    }
}
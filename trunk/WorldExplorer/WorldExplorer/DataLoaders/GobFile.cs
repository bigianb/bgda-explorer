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
    public class GobFile
    {
        public GobFile(EngineVersion engineVersion, string filepath)
        {
            _engineVersion = engineVersion;
            _filepath = filepath;
            FileData = File.ReadAllBytes(filepath);
            ReadDirectory();
            Filename = System.IO.Path.GetFileName(filepath);
        }

        private EngineVersion _engineVersion;
        private string _filepath;

        public String Filename;

        public void ReadDirectory()
        {
            int index = 0;
            String s = DataUtil.GetString(FileData, index);
            while (s.Length > 0) {
                int lmpOffset = BitConverter.ToInt32(FileData, index + 0x20);
                int lmpLen = BitConverter.ToInt32(FileData, index + 0x24);
                Directory[s] = new LmpFile(_engineVersion, s, FileData, lmpOffset, lmpLen);
                index += 0x28;
                s = DataUtil.GetString(FileData, index);
            }
        }

        public byte[] FileData;

        public Dictionary<String, LmpFile> Directory = new Dictionary<string, LmpFile>();       
    }
}

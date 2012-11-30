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
        public String Name;

        /// <summary>
        /// The raw data of the .yak file.
        /// </summary>
        public byte[] FileData;
     
    }
}

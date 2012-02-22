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
using System.Text;
using WorldExplorer.DataLoaders;

namespace WorldExplorer
{
    public class World
    {
        public World(string dataPath, string name)
        {
            DataPath = dataPath;
            Name = name;
        }

        public String DataPath;
        public String Name;
        public List<WorldElement> elements;

        public GobFile WorldGob = null;

        public void Load()
        {
            string gobFilename = DataPath + "\\" + Name + ".gob";
            WorldGob = new GobFile(gobFilename);
        }
    }
}

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
using System.Diagnostics;
using System.Linq;
using System.Text;
using WorldExplorer.DataLoaders;
using WorldExplorer.DataModel;

namespace WorldExplorer
{
    public class World
    {
        public World(EngineVersion engineVersion, string dataPath, string name)
        {
            if (string.IsNullOrEmpty(dataPath))
                Debug.Fail("Datapath is null");
            EngineVersion = engineVersion;
            DataPath = dataPath;
            Name = name;
        }

        public String DataPath;
        public String Name;
        public EngineVersion EngineVersion;

        public GobFile WorldGob = null;
        public WorldTexFile WorldTex = null;

        // The parsed data from the various files.
        public WorldData worldData = null;

        public void Load()
        {
            WorldGob = new GobFile(EngineVersion, System.IO.Path.Combine(DataPath, Name));
            var bareName = System.IO.Path.GetFileNameWithoutExtension(Name) + ".tex";
            WorldTex = new WorldTexFile(EngineVersion, System.IO.Path.Combine(DataPath, bareName));
        }
    }
}

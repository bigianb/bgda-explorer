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

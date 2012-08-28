using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace WorldExplorer.DataLoaders
{
    public class WorldTexFile
    {
        public WorldTexFile(string filepath)
        {
            _filepath = filepath;
            FileData = File.ReadAllBytes(filepath);
            Filename = _filepath.Split(new[] { '\\', '/' }).Last();
        }

        private string _filepath;

        public String Filename;

        public byte[] FileData;
    }
}

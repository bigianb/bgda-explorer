using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorldExplorer.DataLoaders
{
    public class GobFile
    {
        public GobFile(string filepath)
        {
            _filepath = filepath;
            FileData = File.ReadAllBytes(filepath);
            ReadDirectory();
            Filename = _filepath.Split(new[] { '\\', '/' }).Last();
        }

        private string _filepath;

        public String Filename;

        public void ReadDirectory()
        {
            int index = 0;
            String s = GetString(FileData, index);
            while (s.Length > 0) {
                Directory[s] = 0;
                index += 0x28;
                s = GetString(FileData, index);
            }
        }

        public byte[] FileData;

        public Dictionary<String, int> Directory = new Dictionary<string, int>();

        public String GetString(byte[] data, int index)
        {
            StringBuilder sb = new StringBuilder();
            int i = index;
            while (data[i] != 0)
            {
                sb.Append((char)data[i]);
                ++i;
            }
            return sb.ToString();
        }
    }
}

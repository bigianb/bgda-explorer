using System.Collections.Generic;

namespace WorldExplorer.DataLoaders
{
    public static class ScrDecoder
    {
        private const int HEADER_SIZE = 0x60;

        public static Script Decode(byte[] data, int startOffset, int length)
        {
            var reader = new DataReader(data, startOffset, length);

            var script = new Script();

            script.header0 = reader.ReadInt32();


            return script;
        }
    }

    public class Script
    {
        public int header0;

        public string Disassemble()
        {
            return "xx";
        }
    }
}

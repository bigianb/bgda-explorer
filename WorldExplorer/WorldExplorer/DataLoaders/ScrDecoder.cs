using System.Collections.Generic;
using System.Text;

namespace WorldExplorer.DataLoaders
{
    public static class ScrDecoder
    {
        private const int HEADER_SIZE = 0x60;

        public static Script Decode(byte[] data, int startOffset, int length)
        {
            var reader = new DataReader(data, startOffset + HEADER_SIZE, length);

            var script = new Script();

            script.offset0 = reader.ReadInt32();
            script.hw1 = reader.ReadInt16();
            script.hw2 = reader.ReadInt16();
            script.hw3 = reader.ReadInt16();
            script.hw4 = reader.ReadInt16();

            int instructionsOffset = reader.ReadInt32();
            int stringsOffset = reader.ReadInt32();

            script.offset3 = reader.ReadInt32();
            script.offset4 = reader.ReadInt32();
            script.offset5 = reader.ReadInt32();

            script.numInternals = reader.ReadInt32();
            script.offsetInternals = reader.ReadInt32();
            script.numExternals = reader.ReadInt32();
            script.offsetExternals = reader.ReadInt32();

            var internalOffset = startOffset + script.offsetInternals + HEADER_SIZE;
            for (int i=0; i<script.numInternals; ++i)
            {
                var internalReader = new DataReader(data, internalOffset, 0x18);
                var labelAddress = internalReader.ReadInt32();
                string label = internalReader.ReadZString();
                script.internals.Add(label, labelAddress);

                internalOffset += 0x18;
            }

            var externalOffset = startOffset + script.offsetExternals + HEADER_SIZE;
            script.externals = new string[script.numExternals];
            for (int i = 0; i < script.numExternals; ++i)
            {
                var externalReader = new DataReader(data, externalOffset, 0x18);
                var labelAddress = externalReader.ReadInt32();
                if (labelAddress != 0)
                {
                    script.parseWarnings += "found a non-zero external label address\n";
                }
                string label = externalReader.ReadZString();
                script.externals[i] = label;

                externalOffset += 0x18;
            }

            return script;
        }
    }

    public class Script
    {
        public string parseWarnings = "";

        public int offset0;
        public int hw1, hw2, hw3, hw4;

        public int offset3, offset4, offset5;

        // Maps an internal label to an address
        public Dictionary<string, int> internals = new Dictionary<string, int>();
        public int numInternals;
        public int offsetInternals;

        // external references
        public string[] externals;
        public int numExternals;
        public int offsetExternals;

        public string Disassemble()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(parseWarnings)) {
                sb.Append("Warnings:\n").Append(parseWarnings).Append("\n");
            }

            sb.AppendFormat("Offset0: 0x{0}\n", offset0.ToString("X4"));
            sb.AppendFormat("hw1: 0x{0}\n", hw1.ToString("X4"));
            sb.AppendFormat("hw2: 0x{0}\n", hw2.ToString("X4"));
            sb.AppendFormat("hw3: 0x{0}\n", hw3.ToString("X4"));
            sb.AppendFormat("hw4: 0x{0}\n", hw4.ToString("X4"));
            sb.AppendFormat("Offset3: 0x{0}\n", offset3.ToString("X4"));
            sb.AppendFormat("Offset4: 0x{0}\n", offset4.ToString("X4"));
            sb.AppendFormat("Offset5: 0x{0}\n", offset5.ToString("X4"));
            sb.Append("\nInternals\n~~~~~~~~~\n");
            sb.AppendFormat("{0} internals at  0x{1}\n\n", numInternals, offsetInternals.ToString("X4"));
            foreach (string key in internals.Keys)
            {
                sb.AppendFormat("{0}: 0x{1}\n", key, internals[key].ToString("X4"));
            }

            sb.Append("\nExternals\n~~~~~~~~~\n");
            sb.AppendFormat("{0} externals at  0x{1}\n\n", numExternals, offsetExternals.ToString("X4"));
            for (int i=0; i<numExternals; ++i)
            {
                sb.AppendFormat("{0}: {1}\n", i, externals[i]);
            }
            return sb.ToString();
        }
    }
}

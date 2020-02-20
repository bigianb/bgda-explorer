using System;
using System.Collections.Generic;
using System.Text;

namespace WorldExplorer.DataLoaders
{
    public static class ScrDecoder
    {
        private const int HEADER_SIZE = 0x60;

        public static Script Decode(byte[] data, int startOffset, int length)
        {
            if (length == 0)
            {
                return new Script();
            }

            var reader = new DataReader(data, startOffset + HEADER_SIZE, length);

            var script = new Script();

            script.offset0 = reader.ReadInt32();
            script.hw1 = reader.ReadInt16();
            script.hw2 = reader.ReadInt16();
            script.hw3 = reader.ReadInt16();
            script.hw4 = reader.ReadInt16();

            script.instructionsOffset = reader.ReadInt32();
            script.stringsOffset = reader.ReadInt32();

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
                script.internalsByAddr.Add(labelAddress, label);
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

            int stringTableLen = script.offset3 - script.stringsOffset;
            int instructionLen = script.stringsOffset - script.instructionsOffset;
            int thisStringStart = -1;
            var stringReader = new DataReader(data, startOffset + HEADER_SIZE + script.stringsOffset, stringTableLen+1);
            StringBuilder thisString = new StringBuilder();
            for (int i=0; i < stringTableLen; i+= 4)
            {
                if (thisStringStart < 0)
                {
                    thisString = new StringBuilder();
                    thisStringStart = i;
                }
                byte[] bytes = stringReader.ReadBytes(4);
                for (int b=3; b >= 0; --b)
                {
                    if (bytes[b] != 0)
                    {
                        thisString.Append((char)bytes[b]);
                    } else
                    {
                        script.stringTable.Add(thisStringStart, thisString.ToString());
                        thisStringStart = -1;
                        break;
                    }
                }
            }
            DecodeInstructions(script, data, startOffset + HEADER_SIZE + script.instructionsOffset, instructionLen);
            return script;
        }

        private static void DecodeInstructions(Script script, byte[] data, int startOffset, int len)
        {
            var reader = new DataReader(data, startOffset, len);

            for (int i = 0; i < len; i += 4)
            {
                int opcode = reader.ReadInt32();
                Instruction inst = new Instruction
                {
                    opCode = opcode,
                    addr = i
                };
                if (script.internalsByAddr.ContainsKey(i))
                {
                    inst.label = script.internalsByAddr[i];
                }
                ARGS_TYPE type = bgdaOpCodeArgs[opcode];
                switch (type)
                {
                    case ARGS_TYPE.NO_ARGS:

                        break;
                    case ARGS_TYPE.ONE_ARG:
                        inst.args.Add(reader.ReadInt32());
                        i += 4;
                        break;
                    case ARGS_TYPE.ONE_ARG_INSTR:
                        inst.args.Add(reader.ReadInt32());
                        i += 4;
                        break;
                    case ARGS_TYPE.TWO_ARGS:
                        inst.args.Add(reader.ReadInt32());
                        inst.args.Add(reader.ReadInt32());
                        i += 8;
                        break;
                    case ARGS_TYPE.VAR_ARGS:
                        int numArgs = reader.ReadInt32();
                        for (int j=0; j<numArgs-1; ++j)
                        {
                            inst.args.Add(reader.ReadInt32());
                        }
                        i += numArgs * 4;
                        break;
                    case ARGS_TYPE.ARGS_130:
                        int num = reader.ReadInt32(); i += 4;
                        for (int j = 0; j < num; ++j)
                        {
                            inst.args.Add(reader.ReadInt32());
                            inst.args.Add(reader.ReadInt32());
                            i += 8;
                        }
                        inst.args.Add(reader.ReadInt32());
                        i += 4;
                        break;
                }
                script.instructions.Add(inst);
            }
        }

        enum ARGS_TYPE
        {
            NO_ARGS, ONE_ARG, ONE_ARG_INSTR, TWO_ARGS, VAR_ARGS, ARGS_130
        }

        static ARGS_TYPE[] bgdaOpCodeArgs = new ARGS_TYPE[]
            {
            ARGS_TYPE.NO_ARGS,      // 0x00
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,

            ARGS_TYPE.ONE_ARG,      // 0x10
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,

            ARGS_TYPE.ONE_ARG,      // 0x20
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,

            ARGS_TYPE.NO_ARGS,          // 0x30
            ARGS_TYPE.ONE_ARG_INSTR,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.ONE_ARG_INSTR,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG_INSTR,
            ARGS_TYPE.ONE_ARG_INSTR,
            ARGS_TYPE.ONE_ARG_INSTR,
            ARGS_TYPE.ONE_ARG_INSTR,
            ARGS_TYPE.ONE_ARG_INSTR,
            ARGS_TYPE.ONE_ARG_INSTR,
            ARGS_TYPE.ONE_ARG_INSTR,
            ARGS_TYPE.ONE_ARG_INSTR,
            ARGS_TYPE.ONE_ARG_INSTR,
            ARGS_TYPE.ONE_ARG_INSTR,
            ARGS_TYPE.ONE_ARG_INSTR,

            ARGS_TYPE.ONE_ARG_INSTR,    // 0x40
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,

            ARGS_TYPE.NO_ARGS,          //0x50
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,

            ARGS_TYPE.NO_ARGS,      // 0x60
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.NO_ARGS,

            ARGS_TYPE.NO_ARGS,      // 0x70
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.NO_ARGS,
            ARGS_TYPE.ONE_ARG,
            ARGS_TYPE.VAR_ARGS,     // 0x7c
            ARGS_TYPE.TWO_ARGS,     // 0x7d
            ARGS_TYPE.VAR_ARGS,     // 0x7e
            ARGS_TYPE.TWO_ARGS,

            ARGS_TYPE.NO_ARGS,      // 0x80
            ARGS_TYPE.ONE_ARG_INSTR,
            ARGS_TYPE.ARGS_130
            };

    }

    public class Instruction
    {
        public int opCode;
        public int addr;
        public string label;
        public List<int> args = new List<int>();
    }

    public class Script
    {
        public string parseWarnings = "";

        public int offset0;
        public int hw1, hw2, hw3, hw4;

        public int instructionsOffset;
        public int stringsOffset;

        public int offset3, offset4, offset5;

        // Maps an address to a label
        public Dictionary<int, string> internalsByAddr = new Dictionary<int, string>();
        
        public int numInternals;
        public int offsetInternals;

        // external references
        public string[] externals;
        public int numExternals;
        public int offsetExternals;

        public Dictionary<int, string> stringTable = new Dictionary<int, string>();

        public List<Instruction> instructions = new List<Instruction>();

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
            foreach (int key in internalsByAddr.Keys)
            {
                sb.AppendFormat("{0}: 0x{1}\n", key, key.ToString("X4"));
            }

            sb.Append("\nExternals\n~~~~~~~~~\n");
            sb.AppendFormat("{0} externals at  0x{1}\n\n", numExternals, offsetExternals.ToString("X4"));
            for (int i=0; i<numExternals; ++i)
            {
                sb.AppendFormat("{0}: {1}\n", i, externals[i]);
            }
            sb.Append("\nStrings\n~~~~~~~\n");
            foreach (int key in stringTable.Keys)
            {
                sb.AppendFormat("0x{0}: {1}\n", key.ToString("X4"), stringTable[key]);
            }

            sb.Append("\nScript\n~~~~~~\n");
            // Assume that stacks are always deterministic and nothing clever is done with jumps
            Stack<int> stack = new Stack<int>();
            foreach (Instruction inst in instructions)
            {
                string s = DisassembleInstruction(inst, stack);
                sb.Append(s);
                if (s.Length > 0) { sb.Append('\n'); }
            }

            return sb.ToString();
        }

        private string DisassembleInstruction(Instruction inst, Stack<int> stack)
        {
            var sb = new StringBuilder();
            if (!String.IsNullOrEmpty(inst.label))
            {
                sb.Append("\n").Append(inst.label).Append(":\n");
            }
            sb.AppendFormat("{0:x4}  ", inst.addr);
            switch (inst.opCode)
            {
                case 1:
                    sb.AppendFormat("acc = var {0}", inst.args[0]);
                    break;
                case 0x0B:
                    sb.AppendFormat("acc = {0}", inst.args[0]);
                    break;
                case 0x0F:
                    sb.AppendFormat("var {0} = acc", inst.args[0]);
                    break;
                case 0x11:
                    sb.AppendFormat("t4 var {0} = acc", inst.args[0]);
                    break;
                case 0x27:
                    stack.Push(inst.args[0]);
                    sb.AppendFormat("push {0}", inst.args[0]);
                    break;
                case 0x2C:
                    {
                        int numInts = inst.args[0] / 4;
                        for (int i = 0; i < numInts && stack.Count > 0; ++i)
                        {
                            stack.Pop();
                        }
                        sb.AppendFormat("pop {0} bytes", inst.args[0]);
                    }
                    break;
                case 0x2E:
                    sb.AppendFormat("enter");
                    break;
                case 0x30:
                    sb.AppendFormat("return");
                    break;
                case 0x33:
                    sb.AppendFormat("jump to 0x{0:X4}", inst.args[0]);
                    break;
                case 0x35:
                    sb.AppendFormat("jump if acc == 0 to 0x{0:x4}", inst.args[0]);
                    break;
                case 0x36:
                    sb.AppendFormat("jump if acc != 0 to 0x{0:X4}", inst.args[0]);
                    break;
                case 0x54:
                    sb.Append("acc <= 0");
                    break;
                case 0x59:
                    sb.Append("acc = 0");
                    break;
                case 0x5B:
                    sb.AppendFormat("clear var {0}", inst.args[0]);
                    break;
                case 0x7B:
                    sb.AppendFormat(DisasssembleExternal(inst, stack, externals[inst.args[0]]));
                    break;
                case 0x7D:
                    //sb.AppendFormat("debug line {0} [{1}]", inst.args[0], inst.args[1]);
                    break;
                case 0x81:
                    sb.Append("switch(acc)");
                    break;
                default:
                    sb.AppendFormat("unknown, opcode 0x{0:x}", inst.opCode);
                    foreach (int arg in inst.args)
                    {
                        sb.AppendFormat(" 0x{0:x}", arg);
                    }
                    break;
            }

            return sb.ToString();
        }

        private string DisasssembleExternal(Instruction inst, Stack<int> stack, string name)
        {
            var sb = new StringBuilder();
            sb.Append(name).Append(" ");
            switch(name)
            {
                case "addQuest":

                    break;
                case "getv":
                    if (stack.Count >= 2)
                    {
                        var enumerator = stack.GetEnumerator();
                        enumerator.MoveNext();
                        int size = enumerator.Current;
                        enumerator.MoveNext();
                        int stringId = enumerator.Current;
                        
                        sb.Append(stringTable[stringId]);
                    }
                    break;
                case "setv":
                    if (stack.Count >= 3)
                    {
                        var enumerator = stack.GetEnumerator();
                        enumerator.MoveNext();
                        int size = enumerator.Current;
                        enumerator.MoveNext();
                        int stringId = enumerator.Current;
                        enumerator.MoveNext();
                        int val = enumerator.Current;
                        sb.Append(stringTable[stringId]).Append(" = ").Append(val);
                    }
                    break;
            }
            return sb.ToString();
        }
    }
}


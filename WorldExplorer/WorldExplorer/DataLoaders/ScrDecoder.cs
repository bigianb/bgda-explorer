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

            var script = new Script
            {
                offset0 = reader.ReadInt32(),
                hw1 = reader.ReadInt16(),
                hw2 = reader.ReadInt16(),
                hw3 = reader.ReadInt16(),
                hw4 = reader.ReadInt16(),

                instructionsOffset = reader.ReadInt32(),
                stringsOffset = reader.ReadInt32(),

                offset3 = reader.ReadInt32(),
                offset4 = reader.ReadInt32(),
                offset5 = reader.ReadInt32(),

                numInternals = reader.ReadInt32(),
                offsetInternals = reader.ReadInt32(),
                numExternals = reader.ReadInt32(),
                offsetExternals = reader.ReadInt32()
            };

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
                sb.AppendFormat("{0}: 0x{1}\n", internalsByAddr[key], key.ToString("X4"));
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
            if (!string.IsNullOrEmpty(inst.label))
            {
                sb.Append("\n").Append(inst.label).Append(":\n");
            }
            sb.AppendFormat("{0:x4}  ", inst.addr);
            switch (inst.opCode)
            {
                case 1:
                    sb.AppendFormat("a = var {0}", inst.args[0]);
                    break;
                case 2:
                    sb.AppendFormat("s3 = var {0}", inst.args[0]);
                    break;
                case 0x3:
                    sb.AppendFormat("a = t4 var {0}", inst.args[0]);
                    break;
                case 0x4:
                    sb.AppendFormat("s3 = t4 var {0}", inst.args[0]);
                    break;
                case 0x0B:
                    sb.AppendFormat("a = {0}", inst.args[0]);
                    break;
                case 0x0C:
                    sb.AppendFormat("reload a, set s3 to 0x{0:x}", inst.args[0]);
                    break;
                case 0x0F:
                    sb.AppendFormat("var {0} = acc", inst.args[0]);
                    break;
                case 0x11:
                    sb.AppendFormat("t4 var {0} = a", inst.args[0]);
                    break;
                case 0x21:
                    sb.Append("a = s3");
                    break;
                case 0x22:
                    sb.Append("s3 = a");
                    break;
                case 0x23:
                    sb.Append("exch s3, a");
                    break;
                case 0x24:
                    stack.Push(0);
                    sb.Append("push a");
                    break;
                case 0x25:
                    stack.Push(0);
                    sb.Append("push s3");
                    break;
                case 0x27:      
                    stack.Push(inst.args[0]);
                    sb.AppendFormat("push 0x{0:x}", inst.args[0]);
                    break;
                case 0x28:
                    stack.Push(inst.args[0]);   // not correct as it not an immediate
                    sb.AppendFormat("push var {0}", inst.args[0]);
                    break;
                case 0x29:      
                    stack.Push(inst.args[0]);   // not correct as it not an immediate
                    sb.AppendFormat("push t4 var {0}", inst.args[0]);
                    break;
                case 0x2B:
                    {
                        if (stack.Count > 0)
                        {
                            stack.Pop();
                        }
                        sb.Append("pop s3");
                    }
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
                case 0x31:
                    sb.AppendFormat("call 0x{0:x}", inst.args[0]);
                    break;
                case 0x33:
                    sb.AppendFormat("jump to 0x{0:X4}", inst.args[0]);
                    break;
                case 0x35:
                    sb.AppendFormat("jump if a == 0 to 0x{0:x4}", inst.args[0]);
                    break;
                case 0x36:
                    sb.AppendFormat("jump if a != 0 to 0x{0:X4}", inst.args[0]);
                    break;
                case 0x37:
                    sb.AppendFormat("jump if a == s3 to 0x{0:X4}", inst.args[0]);
                    break;
                case 0x38:
                    sb.AppendFormat("jump if a != s3 to 0x{0:X4}", inst.args[0]);
                    break;
                case 0x3D:
                    sb.AppendFormat("jump if a < s3 to 0x{0:X4}", inst.args[0]);
                    break;
                case 0x3E:
                    sb.AppendFormat("jump if a <= s3 to 0x{0:X4}", inst.args[0]);
                    break;
                case 0x3F:
                    sb.AppendFormat("jump if a > s3 to 0x{0:X4}", inst.args[0]);
                    break;
                case 0x40:
                    sb.AppendFormat("jump if a >= s3 to 0x{0:X4}", inst.args[0]);
                    break;
                case 0x4A:
                    sb.Append("a = s3 / a, s3 = remainder");
                    break;
                case 0x54:
                    sb.Append("a <= 0");
                    break;
                case 0x55:
                    sb.Append("neg a");
                    break;
                case 0x57:
                    sb.AppendFormat("a += {0}", inst.args[0]);
                    break;
                case 0x59:
                    sb.Append("a = 0");
                    break;
                case 0x5A:
                    sb.Append("reload a, set s3 to 0");
                    break;
                case 0x5B:
                    sb.AppendFormat("clear var {0}", inst.args[0]);
                    break;
                case 0x5C:
                    sb.AppendFormat("clear t4 var {0}", inst.args[0]);
                    break;
                case 0x69:
                    sb.AppendFormat("(a xor 0x{0:x}) <= 0", inst.args[0]);
                    break;
                case 0x6D:
                    sb.AppendFormat("inc var {0}", inst.args[0]);
                    break;
                case 0x6E:
                    sb.AppendFormat("inc t4 var {0}", inst.args[0]);
                    break;
                case 0x72:
                    sb.AppendFormat("dec var {0}", inst.args[0]);
                    break;
                case 0x7B:
                    sb.AppendFormat(DisasssembleExternal(inst, stack, externals[inst.args[0]]));
                    break;
                case 0x7D:
                    //sb.AppendFormat("debug line {0} [{1}]", inst.args[0], inst.args[1]);
                    break;
                case 0x81:
                    sb.AppendFormat("switch(a) @ 0x{0:x}", inst.args[0]);
                    break;
                case 0x82:
                    sb.Append("switch vector table");
                    foreach (int arg in inst.args)
                    {
                        sb.AppendFormat(" 0x{0:x}", arg);
                    }
                    break;
                default:
                    sb.AppendFormat("unknown opcode 0x{0:x}", inst.opCode);
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
            var enumerator = stack.GetEnumerator();
            enumerator.MoveNext();
            switch (name)
            {
                case "addHelpMessage":
                case "addQuest":
                case "soundSequence":
                case "startPropAnim":
                    PrintSSArgs(enumerator, sb);
                    break;
                case "startDialog":
                    if (stack.Count > 3)
                    {
                        PrintSSIArgs(enumerator, sb);
                    } else
                    {
                        sb.AppendFormat(" ** only {0} entries on the stack", stack.Count);
                    }
                    break;
                
                case "getv":
                case "removeQuest":
                case "stopPropAnim":
                    PrintSArg(enumerator, sb);
                    break;
                case "givePlayerItem":
                case "setNoCollide":
                case "callScript":
                    PrintSIArgs(enumerator, sb);
                    break;
                case "givePlayerExp":
                case "givePlayerGold":
                case "hideMonster":
                    PrintIArg(enumerator, sb);
                    break;
                case "loadMonsterSlot":
                    PrintISIArgs(enumerator, sb);
                    break;
                case "moveTalkTarget":
                    if (stack.Count > 3)
                    {
                        PrintIIIArgs(enumerator, sb);
                    } else
                    {
                        sb.AppendFormat(" ** only {0} entries on the stack", stack.Count);
                    }
                    break;
                case "setTalkTarget":
                    PrintSIIIIIArgs(enumerator, sb);
                    break;
                case "setv":
                    if (stack.Count >= 3)
                    {
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

        private void PrintSArg(Stack<int>.Enumerator enumerator, StringBuilder sb)
        {
            int size = enumerator.Current;
            enumerator.MoveNext();
            int stringId = enumerator.Current;

            sb.Append(stringTable[stringId]);
        }

        private void PrintIArg(Stack<int>.Enumerator enumerator, StringBuilder sb)
        {
            int size = enumerator.Current;
            enumerator.MoveNext();
            int arg1 = enumerator.Current;

            sb.AppendFormat("{0}", arg1);
        }

        private void PrintSSArgs(Stack<int>.Enumerator enumerator, StringBuilder sb)
        {
            int size = enumerator.Current;
            enumerator.MoveNext();
            int body = enumerator.Current;
            enumerator.MoveNext();
            int title = enumerator.Current;
            sb.Append(stringTable[title]).Append(", ").Append(stringTable[body]);
        }

        private void PrintSSIArgs(Stack<int>.Enumerator enumerator, StringBuilder sb)
        {
            int size = enumerator.Current;
            enumerator.MoveNext();
            int arg1 = enumerator.Current;
            enumerator.MoveNext();
            int arg2 = enumerator.Current;
            enumerator.MoveNext();
            int arg3 = enumerator.Current;
            sb.AppendFormat("{0}, {1}, {2}", stringTable[arg1], stringTable[arg2], arg3);
        }

        private void PrintIIIArgs(Stack<int>.Enumerator enumerator, StringBuilder sb)
        {
            int size = enumerator.Current;
            enumerator.MoveNext();
            int arg1 = enumerator.Current;
            enumerator.MoveNext();
            int arg2 = enumerator.Current;
            enumerator.MoveNext();
            int arg3 = enumerator.Current;
            sb.AppendFormat("{0}, {1}, {2}", arg1, arg2, arg3);
        }

        private void PrintSIArgs(Stack<int>.Enumerator enumerator, StringBuilder sb)
        {
            int size = enumerator.Current;
            enumerator.MoveNext();
            int arg1 = enumerator.Current;
            sb.Append(stringTable[arg1]);
            if (enumerator.MoveNext())
            {
                int arg2 = enumerator.Current;
                sb.AppendFormat(", {0}", arg2);
            }
        }
        private void PrintISIArgs(Stack<int>.Enumerator enumerator, StringBuilder sb)
        {
            int size = enumerator.Current;
            enumerator.MoveNext();
            int arg1 = enumerator.Current;
            enumerator.MoveNext();
            int arg2 = enumerator.Current;
            enumerator.MoveNext();
            int arg3 = enumerator.Current;
            sb.AppendFormat("{0}, ", arg1).Append(stringTable[arg2]).AppendFormat(", {0}", arg3);
        }

        private void PrintSIIIIIArgs(Stack<int>.Enumerator enumerator, StringBuilder sb)
        {
            int size = enumerator.Current;
            enumerator.MoveNext();
            int arg1 = enumerator.Current;
            enumerator.MoveNext();
            int arg2 = enumerator.Current;
            enumerator.MoveNext();
            int arg3 = enumerator.Current;
            enumerator.MoveNext();
            int arg4 = enumerator.Current;
            enumerator.MoveNext();
            int arg5 = enumerator.Current;
            enumerator.MoveNext();
            int arg6 = enumerator.Current;
            sb.AppendFormat("{0}, {1}, {2}, {3}, {4}, {5}", stringTable[arg1], arg2, arg3, arg4, arg5, arg6);
        }
    }
}


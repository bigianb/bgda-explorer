using System.Text;

namespace JetBlackEngineLib.Data.Scripting;

public static class ScrDecoder
{
    private enum ARGS_TYPE
    {
        NO_ARGS,
        ONE_ARG,
        ONE_ARG_INSTR,
        TWO_ARGS,
        VAR_ARGS,
        ARGS_130
    }
    
    private const int HEADER_SIZE = 0x60;

    private static readonly ARGS_TYPE[] bgdaOpCodeArgs =
    {
        ARGS_TYPE.NO_ARGS, // 0x00
        ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG,
        ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.NO_ARGS, ARGS_TYPE.ONE_ARG,
        ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG,
        ARGS_TYPE.ONE_ARG, // 0x10
        ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG,
        ARGS_TYPE.ONE_ARG, ARGS_TYPE.NO_ARGS, ARGS_TYPE.ONE_ARG, ARGS_TYPE.NO_ARGS, ARGS_TYPE.ONE_ARG,
        ARGS_TYPE.NO_ARGS, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG,
        ARGS_TYPE.ONE_ARG, // 0x20
        ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS,
        ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.NO_ARGS,
        ARGS_TYPE.NO_ARGS, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS,
        ARGS_TYPE.NO_ARGS, // 0x30
        ARGS_TYPE.ONE_ARG_INSTR, ARGS_TYPE.NO_ARGS, ARGS_TYPE.ONE_ARG_INSTR, ARGS_TYPE.ONE_ARG,
        ARGS_TYPE.ONE_ARG_INSTR, ARGS_TYPE.ONE_ARG_INSTR, ARGS_TYPE.ONE_ARG_INSTR, ARGS_TYPE.ONE_ARG_INSTR,
        ARGS_TYPE.ONE_ARG_INSTR, ARGS_TYPE.ONE_ARG_INSTR, ARGS_TYPE.ONE_ARG_INSTR, ARGS_TYPE.ONE_ARG_INSTR,
        ARGS_TYPE.ONE_ARG_INSTR, ARGS_TYPE.ONE_ARG_INSTR, ARGS_TYPE.ONE_ARG_INSTR, ARGS_TYPE.ONE_ARG_INSTR, // 0x40
        ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG,
        ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS,
        ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS,
        ARGS_TYPE.NO_ARGS, //0x50
        ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS,
        ARGS_TYPE.NO_ARGS, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS,
        ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS,
        ARGS_TYPE.NO_ARGS, // 0x60
        ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS,
        ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG,
        ARGS_TYPE.NO_ARGS, ARGS_TYPE.NO_ARGS, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.NO_ARGS,
        ARGS_TYPE.NO_ARGS, // 0x70
        ARGS_TYPE.NO_ARGS, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.NO_ARGS, ARGS_TYPE.ONE_ARG,
        ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.ONE_ARG, ARGS_TYPE.NO_ARGS,
        ARGS_TYPE.ONE_ARG, ARGS_TYPE.VAR_ARGS, // 0x7c
        ARGS_TYPE.TWO_ARGS, // 0x7d
        ARGS_TYPE.VAR_ARGS, // 0x7e
        ARGS_TYPE.TWO_ARGS, ARGS_TYPE.NO_ARGS, // 0x80
        ARGS_TYPE.ONE_ARG_INSTR, ARGS_TYPE.ARGS_130
    };

    public static Script Decode(byte[] data, int startOffset, int length)
    {
        if (length == 0)
        {
            return new Script();
        }

        DataReader reader = new(data, startOffset + HEADER_SIZE, length);

        Script script = new()
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
        for (var i = 0; i < script.numInternals; ++i)
        {
            DataReader internalReader = new(data, internalOffset, 0x18);
            var labelAddress = internalReader.ReadInt32();
            var label = internalReader.ReadZString();
            script.internalsByAddr.Add(labelAddress, label);
            internalOffset += 0x18;
        }

        var externalOffset = startOffset + script.offsetExternals + HEADER_SIZE;
        script.externals = new string[script.numExternals];
        for (var i = 0; i < script.numExternals; ++i)
        {
            DataReader externalReader = new(data, externalOffset, 0x18);
            var labelAddress = externalReader.ReadInt32();
            if (labelAddress != 0)
            {
                script.parseWarnings += "found a non-zero external label address\n";
            }

            var label = externalReader.ReadZString();
            script.externals[i] = label;

            externalOffset += 0x18;
        }

        var stringTableLen = script.offset3 - script.stringsOffset;
        var instructionLen = script.stringsOffset - script.instructionsOffset;
        var thisStringStart = -1;
        DataReader stringReader =
            new(data, startOffset + HEADER_SIZE + script.stringsOffset, stringTableLen + 1);
        StringBuilder thisString = new();
        for (var i = 0; i < stringTableLen; i += 4)
        {
            if (thisStringStart < 0)
            {
                thisString = new StringBuilder();
                thisStringStart = i;
            }

            var bytes = stringReader.ReadBytes(4);
            for (var b = 3; b >= 0; --b)
            {
                if (bytes[b] != 0)
                {
                    thisString.Append((char)bytes[b]);
                }
                else
                {
                    script.StringTable.Add(thisStringStart, thisString.ToString());
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
        DataReader reader = new(data, startOffset, len);

        for (var i = 0; i < len; i += 4)
        {
            var opcode = reader.ReadInt32();
            Instruction inst = new() {OpCode = opcode, Address = i};
            if (script.internalsByAddr.ContainsKey(i))
            {
                inst.Label = script.internalsByAddr[i];
            }

            var type = bgdaOpCodeArgs[opcode];
            switch (type)
            {
                case ARGS_TYPE.NO_ARGS:

                    break;
                case ARGS_TYPE.ONE_ARG:
                    inst.Args.Add(reader.ReadInt32());
                    i += 4;
                    break;
                case ARGS_TYPE.ONE_ARG_INSTR:
                    inst.Args.Add(reader.ReadInt32());
                    i += 4;
                    break;
                case ARGS_TYPE.TWO_ARGS:
                    inst.Args.Add(reader.ReadInt32());
                    inst.Args.Add(reader.ReadInt32());
                    i += 8;
                    break;
                case ARGS_TYPE.VAR_ARGS:
                    var numArgs = reader.ReadInt32();
                    for (var j = 0; j < numArgs - 1; ++j)
                    {
                        inst.Args.Add(reader.ReadInt32());
                    }

                    i += numArgs * 4;
                    break;
                case ARGS_TYPE.ARGS_130:
                    var num = reader.ReadInt32();
                    i += 4;
                    for (var j = 0; j < num; ++j)
                    {
                        inst.Args.Add(reader.ReadInt32());
                        inst.Args.Add(reader.ReadInt32());
                        i += 8;
                    }

                    inst.Args.Add(reader.ReadInt32());
                    i += 4;
                    break;
            }

            script.instructions.Add(inst);
        }
    }
}
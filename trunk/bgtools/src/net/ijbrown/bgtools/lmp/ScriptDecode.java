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
package net.ijbrown.bgtools.lmp;

import java.io.*;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * Decodes a script file.
 */
public class ScriptDecode
{
    public static void main(String[] args) throws IOException
    {
        ScriptDecode obj = new ScriptDecode();
        obj.decode("tavern");
        obj.decode("cellar1");
    }

    private void decode(String levelName) throws IOException
    {
        String rootDir = "/emu/bgda/BG/DATA_extracted/";
        String lmpName = levelName;

        String outDir = rootDir + lmpName + "/" + lmpName + "_lmp/";

        File outDirFile = new File(outDir);
        outDirFile.mkdirs();

        read("script.scr", outDirFile);
        String txt;
        txt = disassemble(outDirFile);
        writeFile("script.scr.txt", outDirFile, txt);
    }

    private void writeFile(String filename, File outDirFile, String txt) throws IOException
    {
        File file = new File(outDirFile, filename);
        try (PrintWriter writer = new PrintWriter(file)) {
            writer.print(txt);
        }
    }

    private byte[] fileData;

    private void read(String filename, File outDirFile) throws IOException
    {
        File file = new File(outDirFile, filename);
        BufferedInputStream is = new BufferedInputStream(new FileInputStream(file));

        int fileLength = (int) file.length();
        fileData = new byte[fileLength];

        int offset = 0;
        int remaining = fileLength;
        while (remaining > 0) {
            int read = is.read(fileData, offset, remaining);
            if (read == -1) {
                throw new IOException("Read less bytes then expected when reading file");
            }
            remaining -= read;
            offset += read;
        }
    }

    // There is a 0x60 byte header followed by the body.
    private final int bodyOffset = 0x60;

    private String disassemble(File outDirFile)
    {
        StringBuilder sb = new StringBuilder();


        int offset0 = DataUtil.getLEInt(fileData, bodyOffset);
        int hw1 = DataUtil.getLEUShort(fileData, bodyOffset + 0x04);
        int hw2 = DataUtil.getLEUShort(fileData, bodyOffset + 0x06);
        int hw3 = DataUtil.getLEUShort(fileData, bodyOffset + 0x08);
        int hw4 = DataUtil.getLEUShort(fileData, bodyOffset + 0x0A);

        int instructionsOffset = DataUtil.getLEInt(fileData, bodyOffset + 0x0C);
        int stringsOffset = DataUtil.getLEInt(fileData, bodyOffset + 0x10);
        int offset3 = DataUtil.getLEInt(fileData, bodyOffset + 0x14);
        int offset4 = DataUtil.getLEInt(fileData, bodyOffset + 0x18);
        int offset5 = DataUtil.getLEInt(fileData, bodyOffset + 0x1C);

        int numInternals = DataUtil.getLEUShort(fileData, bodyOffset + 0x20);
        int offsetInternals = DataUtil.getLEInt(fileData, bodyOffset + 0x24);

        int numExternals = DataUtil.getLEUShort(fileData, bodyOffset + 0x28);
        int offsetExternals = DataUtil.getLEInt(fileData, bodyOffset + 0x2C);

        sb.append(numInternals).append(" Internals:\n");
        sb.append("~~~~~~~~~~~~\n");
        for (int i = 0; i < numInternals; ++i) {
            printInternal(sb, offsetInternals + 0x18 * i);
        }
        sb.append("\n");

        sb.append(numExternals).append(" Externals:\n");
        sb.append("~~~~~~~~~~~~\n");
        for (int i = 0; i < numExternals; ++i) {
            printExternal(sb, i, offsetExternals + 0x18 * i);
        }
        sb.append("\n");

        StringBuilder sb3 = new StringBuilder();
        dumpStrings(sb3, stringsOffset, offset3 - stringsOffset);

        sb.append("Instructions\n");
        sb.append("~~~~~~~~~~~~\n\n");
        dumpInstructions(sb, instructionsOffset, stringsOffset - instructionsOffset);
        sb.append("\n");

        sb.append("Strings\n");
        sb.append("~~~~~~~\n\n");
        sb.append(sb3);
        sb.append("\n");
        return sb.toString();
    }

    private void dumpStrings(StringBuilder sb, int stringsOffset, int len)
    {
        boolean needsOffset = true;
        int startOffset = 0;
        StringBuilder sb2 = new StringBuilder(64);
        for (int i = 0; i < len; i += 4) {
            if (needsOffset) {
                sb.append(HexUtil.formatHex(i)).append(": ");
                needsOffset = false;
                startOffset = i;
            }
            int ival = DataUtil.getLEInt(fileData, stringsOffset + i + bodyOffset);
            for (int b = 3; b >= 0; --b) {
                int c = (ival >> (b * 8)) & 0xff;
                if (0 == c) {
                    stringTable.put(startOffset, sb2.toString());
                    sb.append(sb2);
                    sb.append("\n");
                    sb2 = new StringBuilder(64);
                    needsOffset = true;
                    break;
                }
                sb2.append((char) c);
            }

        }
    }

    private Map<Integer, String> stringTable = new HashMap<>();

    private void dumpInstructions(StringBuilder sb, int instructionsOffset, int len)
    {
        for (int i = 0; i < len; i += 4) {
            int opcode = DataUtil.getLEInt(fileData, instructionsOffset + i + bodyOffset);
            String label = internalsMap.get(i);
            if (label != null) {
                sb.append("\n").append(label).append(":\n");
            }
            sb.append(HexUtil.formatHex(i)).append(": ");

            int bytesConsumed = disassembleInstruction(sb, opcode, i, instructionsOffset);
            if (bytesConsumed >= 0) {
                i += bytesConsumed;
            } else {
                sb.append(HexUtil.formatHex(opcode));
                if (opcode < opCodeArgs.length && opcode >= 0) {
                    ARGS_TYPE type = opCodeArgs[opcode];
                    switch (type) {
                        case NO_ARGS:
                            break;
                        case ONE_ARG: {
                            i += 4;
                            int arg1 = DataUtil.getLEInt(fileData, instructionsOffset + i + bodyOffset);
                            sb.append(" ").append(HexUtil.formatHex(arg1));
                        }
                        break;
                        case ONE_ARG_INSTR: {
                            i += 4;
                            int arg1 = DataUtil.getLEInt(fileData, instructionsOffset + i + bodyOffset);
                            sb.append(" inst ").append(HexUtil.formatHex(arg1));
                        }
                        break;
                        case TWO_ARGS: {
                            i += 4;
                            int arg1 = DataUtil.getLEInt(fileData, instructionsOffset + i + bodyOffset);
                            sb.append(" ").append(HexUtil.formatHex(arg1));
                            i += 4;
                            int arg2 = DataUtil.getLEInt(fileData, instructionsOffset + i + bodyOffset);
                            sb.append(" ").append(HexUtil.formatHex(arg2));
                        }
                        break;
                        case VAR_ARGS: {
                            i += 4;
                            int num = DataUtil.getLEInt(fileData, instructionsOffset + i + bodyOffset);
                            sb.append(" numArgs=").append(HexUtil.formatHex(num));
                            for (int j = 0; j < num - 1; ++j) {
                                i += 4;
                                int arg = DataUtil.getLEInt(fileData, instructionsOffset + i + bodyOffset);
                                sb.append(" ").append(HexUtil.formatHex(arg));
                            }
                        }
                        break;
                        case ARGS_130: {
                            i += 4;
                            int num = DataUtil.getLEInt(fileData, instructionsOffset + i + bodyOffset);
                            sb.append(" num=").append(HexUtil.formatHex(num));
                            for (int j = 0; j < num; ++j) {
                                i += 4;
                                int arg1 = DataUtil.getLEInt(fileData, instructionsOffset + i + bodyOffset);
                                i += 4;
                                int arg2 = DataUtil.getLEInt(fileData, instructionsOffset + i + bodyOffset);
                                sb.append("\n            ").append(HexUtil.formatHex(arg1)).append(", ").append(arg2);
                            }
                            i += 4;
                            int arg3 = DataUtil.getLEInt(fileData, instructionsOffset + i + bodyOffset);
                            sb.append("\n            ").append(HexUtil.formatHex(arg3));
                        }
                        break;
                    }
                } else {
                    sb.append(" *** Instruction op code out of range");
                }
            }
            sb.append("\n");
        }
    }

    private List<Integer> stack = new ArrayList<>(20);

    private int disassembleInstruction(StringBuilder sb, int opcode, int i, int instructionsOffset)
    {
        int bytesConsumed = -1;
        switch (opcode) {
            case 0xb: {
                bytesConsumed = 4;
                int arg1 = DataUtil.getLEInt(fileData, instructionsOffset + i + bytesConsumed + bodyOffset);
                sb.append("acc = ").append(arg1);
            }
            break;
            case 0xf: {
                bytesConsumed = 4;
                int arg1 = DataUtil.getLEInt(fileData, instructionsOffset + i + bytesConsumed + bodyOffset);
                sb.append("var ").append(arg1).append(" = acc");
            }
            break;
            case 0x27: {
                // pushes a number onto the stack
                bytesConsumed = 4;
                int arg1 = DataUtil.getLEInt(fileData, instructionsOffset + i + bytesConsumed + bodyOffset);
                stack.add(arg1);
                sb.append("push ").append(HexUtil.formatHex(arg1));
            }
            break;
            case 0x2C: {
                // pops a number of bytes off the stack
                bytesConsumed = 4;
                int arg1 = DataUtil.getLEInt(fileData, instructionsOffset + i + bytesConsumed + bodyOffset);
                int numInts = arg1 / 4;
                for (int idx = 0; idx < numInts && stack.size() > 0; ++idx) {
                    stack.remove(stack.size() - 1);
                }
                sb.append("pop bytes ").append(arg1);
            }
            break;
            case 0x2E: {
                // enters a routine
                bytesConsumed = 0;
                stack.clear();
                sb.append("enter");
            }
            break;
            case 0x30: {
                bytesConsumed = 0;
                sb.append("return");
            }
            break;
            case 0x33: {
                bytesConsumed = 4;
                int arg1 = DataUtil.getLEInt(fileData, instructionsOffset + i + bytesConsumed + bodyOffset);
                sb.append("Jump to ").append(HexUtil.formatHexUShort(arg1));
            }
            break;
            case 0x35: {
                bytesConsumed = 4;
                int arg1 = DataUtil.getLEInt(fileData, instructionsOffset + i + bytesConsumed + bodyOffset);
                sb.append("Jump if acc == 0 to ").append(HexUtil.formatHexUShort(arg1));
            }
            break;
            case 0x54: {
                bytesConsumed = 0;
                sb.append("acc = !acc");
            }
            break;
            case 0x59: {
                bytesConsumed = 0;
                sb.append("acc = 0");
            }
            break;
            case 0x5B: {
                bytesConsumed = 4;
                int arg1 = DataUtil.getLEInt(fileData, instructionsOffset + i + bytesConsumed + bodyOffset);
                sb.append("clear local var ").append(HexUtil.formatHex(arg1));
            }
            break;
            case 0x7B: {
                bytesConsumed = 4;
                int arg1 = DataUtil.getLEInt(fileData, instructionsOffset + i + bytesConsumed + bodyOffset);
                String externalName = externalsMap.get(arg1);
                decodeExternalCall(sb, externalName);
            }
            break;
            case 0x7D: {
                bytesConsumed = 4;
                int arg1 = DataUtil.getLEInt(fileData, instructionsOffset + i + bytesConsumed + bodyOffset);
                bytesConsumed += 4;
                int arg2 = DataUtil.getLEInt(fileData, instructionsOffset + i + bytesConsumed + bodyOffset);
                sb.append("Debug line no: ").append(arg1).append("  [").append(arg2).append("]");
            }
            break;
            case 0x81: {
                bytesConsumed = 4;
                int arg1 = DataUtil.getLEInt(fileData, instructionsOffset + i + bytesConsumed + bodyOffset);
                sb.append("switch(acc) ... case statement defs as ").append(HexUtil.formatHex(arg1));
            }
            break;
        }
        return bytesConsumed;
    }

    private void decodeExternalCall(StringBuilder sb, String name)
    {
        sb.append(name);
        switch (name) {
            case "getv":
                if (stack.size() >= 2) {
                    int iarg1 = stack.get(stack.size() - 2);
                    int iarg2 = stack.get(stack.size() - 1);
                    sb.append("(");
                    printStringArg(sb, iarg1);
                    sb.append(", ").append(iarg2).append(")");
                }
                break;
            case "setv":
                if (stack.size() >= 3) {
                    int iarg1 = stack.get(stack.size() - 3);
                    int iarg2 = stack.get(stack.size() - 2);
                    int iarg3 = stack.get(stack.size() - 1);
                    sb.append("(").append(iarg1).append(", ");
                    printStringArg(sb, iarg2);
                    sb.append(", ").append(iarg3).append(")");
                }
                break;
        }
    }

    private void printStringArg(StringBuilder sb, int iarg)
    {
        String sArg = stringTable.get(iarg);
        sb.append(sArg != null ? sArg : "?");
    }

    /**
     * Maps an instruction address to a label.
     */
    private HashMap<Integer, String> internalsMap = new HashMap<>(64);

    private void printInternal(StringBuilder sb, int offset)
    {
        sb.append(HexUtil.formatHexUShort(offset)).append(": ");
        int address = DataUtil.getLEInt(fileData, offset + bodyOffset);
        sb.append(HexUtil.formatHex(address)).append(" - ");
        String label = DataUtil.collectString(fileData, offset + bodyOffset + 4);
        sb.append(label).append("\n");
        internalsMap.put(address, label);
    }

    /**
     * Maps an external id to a label.
     */
    private HashMap<Integer, String> externalsMap = new HashMap<>(64);

    private void printExternal(StringBuilder sb, int id, int offset)
    {
        sb.append(HexUtil.formatHexUShort(id)).append(": ");
        String label = DataUtil.collectString(fileData, offset + bodyOffset + 4);
        sb.append(label).append("\n");
        externalsMap.put(id, label);
    }

    private enum ARGS_TYPE
    {
        NO_ARGS, ONE_ARG, ONE_ARG_INSTR, TWO_ARGS, VAR_ARGS, ARGS_130
    }

    private ARGS_TYPE[] opCodeArgs = new ARGS_TYPE[]
            {
                    ARGS_TYPE.NO_ARGS,
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
                    ARGS_TYPE.ONE_ARG,
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
                    ARGS_TYPE.ONE_ARG,
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
                    ARGS_TYPE.NO_ARGS,
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
                    ARGS_TYPE.ONE_ARG_INSTR,
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
                    ARGS_TYPE.NO_ARGS,
                    ARGS_TYPE.NO_ARGS,
                    ARGS_TYPE.NO_ARGS,
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
                    ARGS_TYPE.NO_ARGS,
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
                    ARGS_TYPE.VAR_ARGS,
                    ARGS_TYPE.TWO_ARGS,
                    ARGS_TYPE.VAR_ARGS,
                    ARGS_TYPE.TWO_ARGS,
                    ARGS_TYPE.NO_ARGS,
                    ARGS_TYPE.ONE_ARG_INSTR,
                    ARGS_TYPE.ARGS_130
            };
}

using System.Text;

namespace JetBlackEngineLib.Data.Scripting;

public class Script
{
    // external references
    public string[]? externals;
    public int hw1, hw2, hw3, hw4;

    public List<Instruction> instructions = new();

    public int instructionsOffset;

    // Maps an address to a label
    public Dictionary<int, string> internalsByAddr = new();
    public int numExternals;

    public int numInternals;

    public int offset0;

    public int offset3, offset4, offset5;
    public int offsetExternals;
    public int offsetInternals;
    public string parseWarnings = "";
    public int stringsOffset;

    public readonly Dictionary<int, string> StringTable = new();

    public string Disassemble()
    {
        StringBuilder sb = new();
        if (!string.IsNullOrEmpty(parseWarnings))
        {
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
        foreach (var key in internalsByAddr.Keys)
        {
            sb.AppendFormat("{0}: 0x{1:X4}\n", internalsByAddr[key], key);
        }

        sb.Append("\nExternals\n~~~~~~~~~\n");
        sb.AppendFormat("{0} externals at  0x{1:X4}\n\n", numExternals, offsetExternals);
        for (var i = 0; i < numExternals; ++i)
        {
            sb.AppendFormat("{0}: {1}\n", i, externals != null ? externals[i] : "NULL");
        }

        sb.Append("\nStrings\n~~~~~~~\n");
        foreach (var key in StringTable.Keys)
        {
            sb.AppendFormat("0x{0:X4}: {1}\n", key, StringTable[key]);
        }

        sb.Append("\nScript\n~~~~~~\n");
        // Assume that stacks are always deterministic and nothing clever is done with jumps
        Stack<int> stack = new();
        foreach (var inst in instructions)
        {
            var s = DisassembleInstruction(inst, stack);
            sb.Append(s);
            if (s.Length > 0)
            {
                sb.Append('\n');
            }
        }

        return sb.ToString();
    }

    private string DisassembleInstruction(Instruction inst, Stack<int> stack)
    {
        StringBuilder sb = new();
        if (!string.IsNullOrEmpty(inst.Label))
        {
            sb.Append("\n").Append(inst.Label).Append(":\n");
        }

        sb.AppendFormat("{0:x4}  ", inst.Address);
        switch (inst.OpCode)
        {
            case 1:
                sb.AppendFormat("a = var {0}", inst.Args[0]);
                break;
            case 2:
                sb.AppendFormat("s3 = var {0}", inst.Args[0]);
                break;
            case 0x3:
                sb.AppendFormat("a = t4 var {0}", inst.Args[0]);
                break;
            case 0x4:
                sb.AppendFormat("s3 = t4 var {0}", inst.Args[0]);
                break;
            case 0x0B:
                sb.AppendFormat("a = {0}", inst.Args[0]);
                break;
            case 0x0C:
                sb.AppendFormat("reload a, set s3 to 0x{0:x}", inst.Args[0]);
                break;
            case 0x0F:
                sb.AppendFormat("var {0} = acc", inst.Args[0]);
                break;
            case 0x11:
                sb.AppendFormat("t4 var {0} = a", inst.Args[0]);
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
                stack.Push(inst.Args[0]);
                sb.AppendFormat("push 0x{0:x}", inst.Args[0]);
                break;
            case 0x28:
                stack.Push(inst.Args[0]); // not correct as it not an immediate
                sb.AppendFormat("push var {0}", inst.Args[0]);
                break;
            case 0x29:
                stack.Push(inst.Args[0]); // not correct as it not an immediate
                sb.AppendFormat("push t4 var {0}", inst.Args[0]);
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
                var numInts = inst.Args[0] / 4;
                for (var i = 0; i < numInts && stack.Count > 0; ++i)
                {
                    stack.Pop();
                }

                sb.AppendFormat("pop {0} bytes", inst.Args[0]);
            }
                break;
            case 0x2E:
                sb.AppendFormat("enter");
                break;
            case 0x30:
                sb.AppendFormat("return");
                break;
            case 0x31:
                sb.AppendFormat("call 0x{0:x}", inst.Args[0]);
                break;
            case 0x33:
                sb.AppendFormat("jump to 0x{0:X4}", inst.Args[0]);
                break;
            case 0x35:
                sb.AppendFormat("jump if a == 0 to 0x{0:x4}", inst.Args[0]);
                break;
            case 0x36:
                sb.AppendFormat("jump if a != 0 to 0x{0:X4}", inst.Args[0]);
                break;
            case 0x37:
                sb.AppendFormat("jump if a == s3 to 0x{0:X4}", inst.Args[0]);
                break;
            case 0x38:
                sb.AppendFormat("jump if a != s3 to 0x{0:X4}", inst.Args[0]);
                break;
            case 0x3D:
                sb.AppendFormat("jump if a < s3 to 0x{0:X4}", inst.Args[0]);
                break;
            case 0x3E:
                sb.AppendFormat("jump if a <= s3 to 0x{0:X4}", inst.Args[0]);
                break;
            case 0x3F:
                sb.AppendFormat("jump if a > s3 to 0x{0:X4}", inst.Args[0]);
                break;
            case 0x40:
                sb.AppendFormat("jump if a >= s3 to 0x{0:X4}", inst.Args[0]);
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
                sb.AppendFormat("a += {0}", inst.Args[0]);
                break;
            case 0x59:
                sb.Append("a = 0");
                break;
            case 0x5A:
                sb.Append("reload a, set s3 to 0");
                break;
            case 0x5B:
                sb.AppendFormat("clear var {0}", inst.Args[0]);
                break;
            case 0x5C:
                sb.AppendFormat("clear t4 var {0}", inst.Args[0]);
                break;
            case 0x69:
                sb.AppendFormat("(a xor 0x{0:x}) <= 0", inst.Args[0]);
                break;
            case 0x6D:
                sb.AppendFormat("inc var {0}", inst.Args[0]);
                break;
            case 0x6E:
                sb.AppendFormat("inc t4 var {0}", inst.Args[0]);
                break;
            case 0x72:
                sb.AppendFormat("dec var {0}", inst.Args[0]);
                break;
            case 0x7B:
                sb.AppendFormat(DisasssembleExternal(inst, stack, externals != null ? externals[inst.Args[0]] : "[NULL]"));
                break;
            case 0x7D:
                //sb.AppendFormat("debug line {0} [{1}]", inst.args[0], inst.args[1]);
                break;
            case 0x81:
                sb.AppendFormat("switch(a) @ 0x{0:x}", inst.Args[0]);
                break;
            case 0x82:
                sb.Append("switch vector table");
                foreach (var arg in inst.Args)
                {
                    sb.AppendFormat(" 0x{0:x}", arg);
                }

                break;
            default:
                sb.AppendFormat("unknown opcode 0x{0:x}", inst.OpCode);
                foreach (var arg in inst.Args)
                {
                    sb.AppendFormat(" 0x{0:x}", arg);
                }

                break;
        }

        return sb.ToString();
    }

    private string DisasssembleExternal(Instruction inst, Stack<int> stack, string name)
    {
        StringBuilder sb = new();
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
                }
                else
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
                }
                else
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
                    var size = enumerator.Current;
                    enumerator.MoveNext();
                    var stringId = enumerator.Current;
                    enumerator.MoveNext();
                    var val = enumerator.Current;
                    sb.Append(StringTable[stringId]).Append(" = ").Append(val);
                }

                break;
        }

        return sb.ToString();
    }

    private void PrintSArg(Stack<int>.Enumerator enumerator, StringBuilder sb)
    {
        var size = enumerator.Current;
        enumerator.MoveNext();
        var stringId = enumerator.Current;

        sb.Append(StringTable[stringId]);
    }

    private void PrintIArg(Stack<int>.Enumerator enumerator, StringBuilder sb)
    {
        var size = enumerator.Current;
        enumerator.MoveNext();
        var arg1 = enumerator.Current;

        sb.AppendFormat("{0}", arg1);
    }

    private void PrintSSArgs(Stack<int>.Enumerator enumerator, StringBuilder sb)
    {
        var size = enumerator.Current;
        enumerator.MoveNext();
        var body = enumerator.Current;
        enumerator.MoveNext();
        var title = enumerator.Current;
        sb.Append(StringTable[title]).Append(", ").Append(StringTable[body]);
    }

    private void PrintSSIArgs(Stack<int>.Enumerator enumerator, StringBuilder sb)
    {
        var size = enumerator.Current;
        enumerator.MoveNext();
        var arg1 = enumerator.Current;
        enumerator.MoveNext();
        var arg2 = enumerator.Current;
        enumerator.MoveNext();
        var arg3 = enumerator.Current;
        sb.AppendFormat("{0}, {1}, {2}", StringTable[arg1], StringTable[arg2], arg3);
    }

    private void PrintIIIArgs(Stack<int>.Enumerator enumerator, StringBuilder sb)
    {
        var size = enumerator.Current;
        enumerator.MoveNext();
        var arg1 = enumerator.Current;
        enumerator.MoveNext();
        var arg2 = enumerator.Current;
        enumerator.MoveNext();
        var arg3 = enumerator.Current;
        sb.AppendFormat("{0}, {1}, {2}", arg1, arg2, arg3);
    }

    private void PrintSIArgs(Stack<int>.Enumerator enumerator, StringBuilder sb)
    {
        var size = enumerator.Current;
        enumerator.MoveNext();
        var arg1 = enumerator.Current;
        sb.Append(StringTable[arg1]);
        if (enumerator.MoveNext())
        {
            var arg2 = enumerator.Current;
            sb.AppendFormat(", {0}", arg2);
        }
    }

    private void PrintISIArgs(Stack<int>.Enumerator enumerator, StringBuilder sb)
    {
        var size = enumerator.Current;
        enumerator.MoveNext();
        var arg1 = enumerator.Current;
        enumerator.MoveNext();
        var arg2 = enumerator.Current;
        enumerator.MoveNext();
        var arg3 = enumerator.Current;
        sb.AppendFormat("{0}, ", arg1).Append(StringTable[arg2]).AppendFormat(", {0}", arg3);
    }

    private void PrintSIIIIIArgs(Stack<int>.Enumerator enumerator, StringBuilder sb)
    {
        var size = enumerator.Current;
        enumerator.MoveNext();
        var arg1 = enumerator.Current;
        enumerator.MoveNext();
        var arg2 = enumerator.Current;
        enumerator.MoveNext();
        var arg3 = enumerator.Current;
        enumerator.MoveNext();
        var arg4 = enumerator.Current;
        enumerator.MoveNext();
        var arg5 = enumerator.Current;
        enumerator.MoveNext();
        var arg6 = enumerator.Current;
        sb.AppendFormat("{0}, {1}, {2}, {3}, {4}, {5}", StringTable[arg1], arg2, arg3, arg4, arg5, arg6);
    }
}
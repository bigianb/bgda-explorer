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

using System.Text;

namespace JetBlackEngineLib.Data.Models;

public class GIFTag
{
    public const int Size = 0x10;
        
    /// <summary>
    /// End of packet flag.
    /// </summary>
    public bool eop;
        
    public int flg;

    /// <summary>
    /// Data per register to transfer.
    /// </summary>
    public int nloop;
        
    /// <summary>
    /// Number of registers.
    /// </summary>
    public int nreg;
        
    /// <summary>
    /// Enable prim field.
    /// </summary>
    public bool pre;
        
    /// <summary>
    /// If <see cref="pre"/> is true, data to be sent to GS PRIM register.
    /// </summary>
    public int prim;
        
    /// <summary>
    /// Register fields values.
    /// </summary>
    public int[]? regs;

    public int Length
    {
        get
        {
            if (2 == flg)
                // IMAGE mode
            {
                return (nloop + 1) * 0x10;
            }

            return ((nloop * nreg) + 1) * 0x10;
        }
    }

    /// <summary>
    /// Indicates if the GIF <c>flg</c> parameter equals <c>2</c> which means "IMAGE".
    /// </summary>
    public bool IsImage => flg == 2;

    public void Parse(ReadOnlySpan<byte> data)
    {
        var low32 = DataUtil.GetLeInt(data, 0);
        nloop = low32 & 0x7FFF;
        eop = (low32 & 0x8000) == 0x8000;

        var next32 = DataUtil.GetLeInt(data, 4);

        // bit 32 is bit 0 of next 32
        pre = ((next32 >> (46 - 32)) & 1) == 1;
        // prim 11 bits 47 - 57
        prim = (next32 >> (47 - 32)) & 0x3FF;
        flg = (next32 >> (58 - 32)) & 0x3;
        nreg = (next32 >> (60 - 32)) & 0xf;

        if (0 == nreg)
        {
            nreg = 16;
        }

        var regs64 = DataUtil.GetLeInt(data, 8);
        var regs96 = DataUtil.GetLeInt(data, 12);

        regs = new int[nreg];
        for (var reg = 0; reg < nreg; ++reg)
        {
            var rgs = reg > 7 ? regs96 : regs64;
            regs[reg] = (rgs >> ((reg & 7) * 4)) & 0x0f;
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append("nloop: ").Append(nloop).Append(", ");
        sb.Append("eop: ").Append(eop).Append(", ");
        sb.Append("pre: ").Append(pre).Append(", ");
        sb.Append("prim: ").Append(HexUtil.FormatHex(prim)).Append(", ");
        sb.Append("flg: ").Append(FlagString()).Append(", ");
        sb.Append("nreg: ").Append(nreg).Append(", ");
        sb.Append("regs: ");
        for (var r = 0; r < nreg; ++r)
        {
            sb.Append(regs?[r] ?? 0);
            if (r != nreg)
            {
                sb.Append(", ");
            }
        }

        return sb.ToString();
    }

    private string FlagString()
    {
        switch (flg)
        {
            case 0:
                return "PACKED";
            case 1:
                return "REGLIST";
            case 2:
                return "IMAGE";
            case 3:
                return "DISABLE";
            default:
                return "ERROR";
        }
    }
}
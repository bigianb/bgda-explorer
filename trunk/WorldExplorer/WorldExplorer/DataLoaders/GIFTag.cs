using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorldExplorer.DataLoaders
{
    class GIFTag
    {
    public void parse(byte[] data, int idx)
    {
        int low32 = DataUtil.getLEInt(data, idx);
        nloop = low32 & 0x7FFF;
        eop = (low32 & 0x8000) == 0x8000;

        int next32 = DataUtil.getLEInt(data, idx + 4);

        // bit 32 is bit 0 of next 32
        pre = ((next32 >> (46 - 32)) & 1) == 1;
        // prim 11 bits 47 - 57
        prim = ((next32 >> (47 - 32)) & 0x3FF);
        flg = ((next32 >> (58 - 32)) & 0x3);
        nreg = ((next32 >> (60 - 32)) & 0xf);

        if (0 == nreg){
            nreg = 16;
        }
        int regs64 = DataUtil.getLEInt(data, idx + 8);
        int regs96 = DataUtil.getLEInt(data, idx + 12);

        regs = new int[nreg];
        for (int reg=0; reg < nreg; ++reg){
            int rgs = reg > 7 ? regs96 : regs64;
            regs[reg] = (rgs >> ((reg & 7) * 4)) & 0x0f;
        }
    }

    public override String ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("nloop: ").Append(nloop).Append(", ");
        sb.Append("eop: ").Append(eop).Append(", ");
        sb.Append("pre: ").Append(pre).Append(", ");
        sb.Append("prim: ").Append(HexUtil.formatHex(prim)).Append(", ");
        sb.Append("flg: ").Append(flagString()).Append(", ");
        sb.Append("nreg: ").Append(nreg).Append(", ");
        sb.Append("regs: ");
        for (int r=0; r<nreg; ++r){
            sb.Append(regs[r]);
            if (r != nreg){
                sb.Append(", ");
            }
        }

        return sb.ToString();
    }

    public String flagString()
    {
        switch (flg) {
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

    public int nloop;
    public bool eop;
    public bool pre;
    public int prim;
    public int flg;
    public int nreg;
    public int[] regs;
    }
}

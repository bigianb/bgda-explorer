/*  Copyright (C) 2011 Ian Brown

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

/**
 * A GIF tag.
 */
public class GIFTag
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

    public int getLength()
    {
        if (2 == flg){
            // IMAGE mode
            return (nloop+1)*0x10;
        } else {
            return (nloop*nreg+1)*0x10;
        }
    }

    @Override
    public String toString()
    {
        StringBuilder sb = new StringBuilder();
        sb.append("nloop: ").append(nloop).append(", ");
        sb.append("eop: ").append(eop).append(", ");
        sb.append("pre: ").append(pre).append(", ");
        sb.append("prim: ").append(HexUtil.formatHex(prim)).append(", ");
        sb.append("flg: ").append(flagString()).append(", ");
        sb.append("nreg: ").append(nreg).append(", ");
        sb.append("regs: ");
        for (int r=0; r<nreg; ++r){
            sb.append(regs[r]);
            if (r != nreg){
                sb.append(", ");
            }
        }

        return sb.toString();
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

    int nloop;
    boolean eop;
    boolean pre;
    int prim;
    int flg;
    int nreg;
    int regs[];
}

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

namespace WorldExplorer.DataLoaders
{
    public class GIFTag
    {
        public const int Size = 0x10;

        public void parse(byte[] data, int idx)
        {
            var low32 = DataUtil.getLEInt(data, idx);
            nloop = low32 & 0x7FFF;
            eop = (low32 & 0x8000) == 0x8000;

            var next32 = DataUtil.getLEInt(data, idx + 4);

            // bit 32 is bit 0 of next 32
            pre = ((next32 >> (46 - 32)) & 1) == 1;
            // prim 11 bits 47 - 57
            prim = ((next32 >> (47 - 32)) & 0x3FF);
            flg = ((next32 >> (58 - 32)) & 0x3);
            nreg = ((next32 >> (60 - 32)) & 0xf);

            if (0 == nreg)
            {
                nreg = 16;
            }
            var regs64 = DataUtil.getLEInt(data, idx + 8);
            var regs96 = DataUtil.getLEInt(data, idx + 12);

            regs = new int[nreg];
            for (var reg = 0; reg < nreg; ++reg)
            {
                var rgs = reg > 7 ? regs96 : regs64;
                regs[reg] = (rgs >> ((reg & 7) * 4)) & 0x0f;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("nloop: ").Append(nloop).Append(", ");
            sb.Append("eop: ").Append(eop).Append(", ");
            sb.Append("pre: ").Append(pre).Append(", ");
            sb.Append("prim: ").Append(HexUtil.formatHex(prim)).Append(", ");
            sb.Append("flg: ").Append(flagString()).Append(", ");
            sb.Append("nreg: ").Append(nreg).Append(", ");
            sb.Append("regs: ");
            for (var r = 0; r < nreg; ++r)
            {
                sb.Append(regs[r]);
                if (r != nreg)
                {
                    sb.Append(", ");
                }
            }

            return sb.ToString();
        }

        public int Length
        {
            get
            {
                if (2 == flg)
                {
                    // IMAGE mode
                    return (nloop + 1) * 0x10;
                }
                else
                {
                    return (nloop * nreg + 1) * 0x10;
                }
            }
        }

        /// <summary>
        /// Indicates if the GIF <c>flg</c> parameter equals <c>2</c> which means "IMAGE".
        /// </summary>
        public bool IsImage => flg == 2;

        private string flagString()
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

        public int nloop;
        public bool eop;
        public bool pre;
        public int prim;
        public int flg;
        public int nreg;
        public int[] regs;
    }
}

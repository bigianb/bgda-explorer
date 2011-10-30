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

        int next32 = DataUtil.getLEInt(data, idx+4);

        // bit 32 is bit 0 of next 32
        pre = ((next32 >> (46-32)) & 1) == 1;
        // prim 11 bits 47 - 57
        prim = ((next32 >> (47 - 32)) & 0x7FF);
        flg = ((next32 >> (58 - 32)) & 0x3);
        nreg = ((next32 >> (60 - 32)) & 0xf);
    }

    int nloop;
    boolean eop;
    boolean pre;
    int prim;
    int flg;
    int nreg;
}

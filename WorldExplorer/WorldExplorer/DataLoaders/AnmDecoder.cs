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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace WorldExplorer.DataLoaders
{
    public class AnmDecoder
    {
        public static AnimData Decode(byte[] data, int startOffset, int length)
        {
            int endIndex = startOffset + length;
            AnimData animData = new AnimData();
            animData.NumBones = DataUtil.getLEInt(data, startOffset);
            animData.Offset4Val = DataUtil.getLEInt(data, startOffset + 4);
            int offset8Val = startOffset + DataUtil.getLEInt(data, startOffset + 8);

            AnimFrame frame = new AnimFrame();

            frame.x = DataUtil.getLEShort(data, offset8Val) / 64.0;
            frame.y = DataUtil.getLEShort(data, offset8Val+2) / 64.0;
            frame.z = DataUtil.getLEShort(data, offset8Val+4) / 64.0;

            frame.a = DataUtil.getLEShort(data, offset8Val + 6) / 4096.0;
            frame.b = DataUtil.getLEShort(data, offset8Val + 8) / 4096.0;
            frame.c = DataUtil.getLEShort(data, offset8Val + 10) / 4096.0;
            frame.d = DataUtil.getLEShort(data, offset8Val + 12) / 4096.0;

            animData.Frames.Add(frame);

            return animData;
        }
    }

    public class AnimFrame
    {
        public double x, y, z;
        public double a, b, c, d;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("AnimFrame: (").Append(x).Append(", ");
            sb.Append(y).Append(", ");
            sb.Append(z).Append(") (");
            sb.Append(a).Append(", ");
            sb.Append(b).Append(", ");
            sb.Append(c).Append(", ");
            sb.Append(d).Append(")");
            return sb.ToString();
        }
    }

    public class AnimData
    {
        public int NumBones;
        public int Offset4Val;

        public List<AnimFrame> Frames = new List<AnimFrame>();

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Num Bones = ").Append(NumBones).Append("\n");
            sb.Append("Offset 4 val = ").Append(Offset4Val).Append("\n");
            foreach (var frame in Frames) {
                sb.Append(frame.ToString());
            }
            return sb.ToString();
        }
    }

}

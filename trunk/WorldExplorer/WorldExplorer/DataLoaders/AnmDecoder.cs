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
            animData.Offset14Val = DataUtil.getLEInt(data, startOffset + 0x14);
            animData.Offset18Val = DataUtil.getLEInt(data, startOffset + 0x18);
            int offset8Val = startOffset + DataUtil.getLEInt(data, startOffset + 8);

            for (int bone = 0; bone < animData.NumBones; ++bone) {
                AnimFrame frame = new AnimFrame();
                int frameOff = offset8Val + bone * 0x0e;

                frame.x = DataUtil.getLEShort(data, frameOff) / 64.0;
                frame.y = DataUtil.getLEShort(data, frameOff + 2) / 64.0;
                frame.z = DataUtil.getLEShort(data, frameOff + 4) / 64.0;

                frame.a = DataUtil.getLEShort(data, frameOff + 6) / 4096.0;
                frame.b = DataUtil.getLEShort(data, frameOff + 8) / 4096.0;
                frame.c = DataUtil.getLEShort(data, frameOff + 0xA) / 4096.0;
                frame.d = DataUtil.getLEShort(data, frameOff + 0xC) / 4096.0;

                animData.Frames.Add(frame);
            }
            int otherOff = offset8Val + animData.NumBones * 0x0e;
            var sb = new StringBuilder();
            while (otherOff < endIndex) {
                sb.Append("Count: ").Append(data[otherOff++]);
                byte byte2 = data[otherOff++];
                int bone = byte2 & 0x3f;
                sb.Append(", Bone: ").Append(bone);
                // bit 7 specifies whether to read 4 (set) or 3 elements following
                // bit 6 specifies whether they are shorts or bytes (set).
                if ((byte2 & 0x80) == 0x80) {
                    int a, b, c, d;
                    if ((byte2 & 0x40) == 0x40) {
                        a = data[otherOff++];
                        b = data[otherOff++];
                        c = data[otherOff++];
                        d = data[otherOff++];
                    } else {
                        a = DataUtil.getLEShort(data, otherOff);
                        b = DataUtil.getLEShort(data, otherOff+2);
                        c = DataUtil.getLEShort(data, otherOff+4);
                        d = DataUtil.getLEShort(data, otherOff+6);
                        otherOff += 8;
                    }
                    sb.Append(", (").Append(a).Append(", ").Append(b).Append(", ").Append(c).Append(", ").Append(d).Append(")");
                } else {
                    int x, y, z;
                    if ((byte2 & 0x40) == 0x40) {
                        x = data[otherOff++];
                        y = data[otherOff++];
                        z = data[otherOff++];
                    } else {
                        x = DataUtil.getLEShort(data, otherOff);
                        y = DataUtil.getLEShort(data, otherOff + 2);
                        z = DataUtil.getLEShort(data, otherOff + 4);
                        otherOff += 6;
                    }
                    sb.Append(", (").Append(x).Append(", ").Append(y).Append(", ").Append(z).Append(")");
                }
                sb.Append("\n");
            }
            animData.Other = sb.ToString();
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
        public int Offset14Val;
        public int Offset18Val;     // These are 4 bytes which are all ored together

        public string Other;

        public List<AnimFrame> Frames = new List<AnimFrame>();

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Num Bones = ").Append(NumBones).Append("\n");
            sb.Append("Offset 4 val = ").Append(Offset4Val).Append("\n");
            sb.Append("Offset 0x14 val = ").Append(Offset14Val).Append("\n");
            sb.Append("Offset 0x18 val = ").Append(Offset18Val).Append("\n");
            foreach (var frame in Frames) {
                sb.Append(frame.ToString()).Append("\n");
            }
            sb.Append(Other);
            return sb.ToString();
        }
    }

}

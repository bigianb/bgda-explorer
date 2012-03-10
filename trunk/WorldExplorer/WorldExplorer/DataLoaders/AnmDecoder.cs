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
            return animData;
        }
    }

    public class AnimData
    {
        public int NumBones;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Num Bones = ").Append(NumBones).Append("\n");
            return sb.ToString();
        }
    }

}

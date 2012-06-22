using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace WorldExplorer.DataModel
{
    /// <summary>
    /// Specifies the position and rotation for a single mesh/bone.
    /// </summary>
    class AnimFrame
    {
        public int FrameNum;
        public Vector3D Position;
        public Quaternion Rotation;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WorldExplorer.DataModel
{
    public struct VertexWeight
    {
        public int startVertex;
        public int endVertex;
        public int bone1;
        public int bone2;
        public int bone3;
        public int bone4;
        public int boneWeight1;
        public int boneWeight2;
        public int boneWeight3;
        public int boneWeight4;

        // Copy Constructor
        public VertexWeight(VertexWeight vw)
        {
            startVertex = vw.startVertex;
            endVertex = vw.endVertex;
            bone1 = vw.bone1;
            bone2 = vw.bone2;
            bone3 = vw.bone3;
            bone4 = vw.bone4;
            boneWeight1 = vw.boneWeight1;
            boneWeight2 = vw.boneWeight2;
            boneWeight3 = vw.boneWeight3;
            boneWeight4 = vw.boneWeight4;
        }

        public override String ToString()
        {
            String s = "Vertex Weight: " + startVertex + " -> " + endVertex + ", bone1=" + bone1 + ", weight=" + boneWeight1;
            if (bone2 != 255) {
                s += "; bone2=" + bone2 + ", weight=" + boneWeight2;
            }
            if (bone3 != 255) {
                s += "; bone3=" + bone3 + ", weight=" + boneWeight3;
            }
            if (bone4 != 255) {
                s += "; bone4=" + bone4 + ", weight=" + boneWeight4;
            }
            return s;
        }
    }
}

using System;

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

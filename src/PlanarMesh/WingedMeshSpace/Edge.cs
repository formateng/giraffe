using System;
using Rhino.Geometry;

namespace Giraffe.WingedMeshSpace
{
    public class Edge
    {
        public int index;

        public Vertex beginVert;
        public Vertex endVert;

        public Face rightFace;
        public Face leftFace;

        public Boolean boundaryEdge;

        public Edge(int tIndex, Vertex tBeginVert, Vertex tEndvert)
        {
            index = tIndex;
            beginVert = tBeginVert;
            endVert = tEndvert;
            beginVert.connectedEdges.Add(this);
            endVert.connectedEdges.Add(this);
            boundaryEdge = true;
            if (beginVert.index == endVert.index)
            {

            }
        }

        public Line convertEdgeToRhinoLine()
        {
            if (beginVert.index == endVert.index)
            {
                //with the offset mesh something is going wrong! something to do with the rebuild after the remeshing?
            }
            return new Line(UsefulFunctions.convertVertexToPoint3d(beginVert),UsefulFunctions.convertVertexToPoint3d(endVert));
        }


        internal float calculateThicknessOffset(float thickness, float minDistance)
        {
            //get the normals and the middle normal - should walk for both directions, should do, they are just vectors.
            Vector3d n1 = leftFace.faceNormal;
            Vector3d n2 = rightFace.faceNormal;
            Vector3d nMid = Vector3d.Add(n1, n2);
            nMid.Unitize();

            double alpha = Vector3d.VectorAngle(n1, nMid);

            double lFull = ((minDistance / 2) / Math.Sin(alpha)) + (thickness/2);

            double offsetDistance = (lFull * Math.Sin(alpha)) / (Math.Sin(Math.PI / 2 - alpha));
            return (float)offsetDistance;
        }
    }
}

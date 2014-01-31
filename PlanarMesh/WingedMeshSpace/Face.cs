using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using Rhino.Geometry;



namespace PlanarMesh.WingedMeshSpace
{
    public class Face
    {
        public int index;
        public List<Vertex> faceVerts;//anticlockwise collection of face vertices
        public List<Edge> faceEdges;//anticlockwise collection of face edges (not necessarily go begin to end to begin...) these would be a good thing to check!
        public Vector3d faceNormal;
        public Point3d faceCentre;
        public WingedMesh refMesh;
        public List<int> surroundingFaceIndices;

        public Face(int tIndex, List<Vertex> tFaceVerts, Vector3f tNormal, Point3f tFaceCentre, WingedMesh tMesh)
            //add something which stops you ever sending in only 1 or 2 verts?
        {
            index = tIndex;
            faceVerts = tFaceVerts;
            faceNormal = tNormal;
            faceNormal.Unitize();
            faceCentre = tFaceCentre;
            faceEdges = new List<Edge>();
            refMesh = tMesh;
            initialiseEdges();
        }

        public Face(int tIndex, List<Vertex> tFaceVerts, WingedMesh tMesh)
            : this(tIndex, tFaceVerts, new Vector3f(0.0f, 0.0f, 0.0f), calculateCentre(tFaceVerts), tMesh)
        {
            //just calls the other one.  the normal is not correct - perhaps implement a normal calculation but as it is n-gon there is no proper normall..
        }

        private static Point3f calculateCentre(List<Vertex> tFaceVerts)
        {
            Vector3d centreVector = new Vector3d(0, 0, 0);
            for (int i = 0; i < tFaceVerts.Count; i++)
            {
                Vector3d toAdd = new Vector3d(tFaceVerts[i].position);
                centreVector = Vector3d.Add(centreVector, toAdd);
            }
            centreVector = Vector3d.Divide(centreVector,tFaceVerts.Count);
            return new Point3f((float) centreVector.X, (float) centreVector.Y, (float) centreVector.Z);
        }

        private Vector3d calculateNormal(Point3d origin, Point3d xPoint, Point3d yPoint)
        {
            Plane plane = new Plane(origin, xPoint, yPoint);
            return plane.Normal;
        }

        public void setSurroundingFaces()
        {
            surroundingFaceIndices = new List<int>();
            for (int i = 0; i < faceEdges.Count; i++)
            {
                Edge edge = faceEdges[i];
                if (edge.leftFace==null || edge.rightFace == null) {
                    surroundingFaceIndices.Add(-1);
                }
                else if (edge.leftFace.index == index)
                {
                    surroundingFaceIndices.Add(edge.rightFace.index);
                }
                else
                {
                    surroundingFaceIndices.Add(edge.leftFace.index);
                }
            }
        }
        
        void initialiseEdges()
        {
            int numVerts = faceVerts.Count;
            for (int i = 0; i < numVerts; i++)
            {
                Vertex vert1 = faceVerts[i];
                Vertex vert2 = faceVerts[(i + 1) % numVerts];
                int isEdgeNew = refMesh.isEdgeNew(vert1, vert2);
                if (isEdgeNew == -1)
                {
                    refMesh.addEdge(vert1, vert2);
                    refMesh.edges[refMesh.edges.Count - 1].leftFace = this;
                    faceEdges.Add(refMesh.edges[refMesh.edges.Count - 1]);
                    refMesh.edges[refMesh.edges.Count - 1].beginVert.addFaceifNew(this);
                    refMesh.edges[refMesh.edges.Count - 1].endVert.addFaceifNew(this);
                }
                else
                {
                    refMesh.edges[isEdgeNew].rightFace = this;
                    refMesh.edges[isEdgeNew].boundaryEdge = false;//must now not be a boundary edge!
                    faceEdges.Add(refMesh.edges[isEdgeNew]);
                    refMesh.edges[isEdgeNew].beginVert.addFaceifNew(this);
                    refMesh.edges[isEdgeNew].endVert.addFaceifNew(this);
                }
            }
        }

        public Polyline convertFaceToPolyLine()
        {
            Polyline borderLine = new Polyline();
            foreach (Vertex faceVertex in faceVerts)
            {
                borderLine.Add(faceVertex.position.X, faceVertex.position.Y, faceVertex.position.Z);
            }
            //close the curve.. is this properly closing it - yes!
            borderLine.Add(faceVerts[0].position.X, faceVerts[0].position.Y, faceVerts[0].position.Z);
            return borderLine;
        }

        public Polyline convertFaceToOffsetPolyline(List<float> offSetsInOrder)
        {
            //definitely need to extend the lines!
            List<Line> linesInOrder = new List<Line>();
            for (int i = 0; i < faceEdges.Count; i++)
            {
                Line line = faceEdges[i].convertEdgeToRhinoLine();
                Vector3d insetVector = line.Direction;
                insetVector.Unitize();
                if (faceEdges[i].rightFace==this) {//make sure it is going in the right direction with respect to the face normal to point inwards..
                    insetVector.Reverse();
                }

                insetVector = Vector3d.CrossProduct(faceNormal, insetVector);
                insetVector = Vector3d.Multiply(offSetsInOrder[faceEdges[i].index],insetVector);
                Transform transform = Transform.Translation(insetVector);
                line.Transform(transform);
                line.Extend(line.Length/2,line.Length/2);
                linesInOrder.Add(line);
            }

            List<Point3d> offsetBoundaryVerts = new List<Point3d>();

            for (int i = 0; i < linesInOrder.Count; i++)
            {
                Line thisLine = linesInOrder[i];
                Line nextLine = linesInOrder[(i + 1) % linesInOrder.Count];

                double a, b;
                if (!Rhino.Geometry.Intersect.Intersection.LineLine(thisLine, nextLine, out a, out b))
                {
                }//means intersection failed
                offsetBoundaryVerts.Add(thisLine.PointAt(a));
            }

            //close the curve
            Polyline boundary = new Polyline(offsetBoundaryVerts);
            boundary.Add(boundary.First);

            //maybe check for self intersection here? and then delete verts? could get very complicated

            return boundary;
        }

        public void calculateFaceNormal()
        {
            //using newell method to calulcate the normal

            double normalX = 0.0;
            double normalY = 0.0;
            double normalZ = 0.0;



            for (int i = 0; i < faceVerts.Count; i++)
            {
                Vector3f positionCurrent = faceVerts[i].position;
                Vector3f positionNext = faceVerts[(i + 1) % faceVerts.Count].position;

                normalX += (positionCurrent.Y - positionNext.Y) * (positionCurrent.Z + positionNext.Z);
                normalY += (positionCurrent.Z - positionNext.Z) * (positionCurrent.X + positionNext.X);
                normalZ += (positionCurrent.X - positionNext.X) * (positionCurrent.Y + positionNext.Y);
            }

            faceNormal = new Vector3d(normalX, normalY, normalZ);
            faceNormal.Unitize();
        }
    }
}

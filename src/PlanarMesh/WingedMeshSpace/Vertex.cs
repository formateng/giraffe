using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace Giraffe.WingedMeshSpace
{
    public class Vertex
    {
        public int index;
        public List<Edge> connectedEdges;
        public List<Face> connectedFaces;
        public Vector3f position;
        public Boolean boundaryVert;
        public Boolean edgesAndFacesSorted;

        public Vertex(int tIndex, Vector3f tPosition)
        {
            index = tIndex;
            position = tPosition;
            connectedEdges = new List<Edge>();
            connectedFaces = new List<Face>();
            edgesAndFacesSorted = false;
        }

        public void setIfBoundaryVert()
        {
            boundaryVert = false;
            foreach (Edge edge in connectedEdges)
            {
                if (edge.boundaryEdge)
                {
                    boundaryVert = true;
                    break;
                }
            }
        }

        public void addFaceifNew(Face faceToAdd)
        {
            Boolean isNew = true;
            for (int i = 0; i < connectedFaces.Count; i++)
            {
                if (faceToAdd.index == connectedFaces[i].index)
                {
                    isNew = false;
                }
            }
            if (isNew) {
                connectedFaces.Add(faceToAdd);
            }
        }

        public void sortEdgesAndFaceAntiClockwise()
        {
            sortEdgesAntiClockwise();
            sortFacesAntiClockwise();
            edgesAndFacesSorted = true;
        }

        private void sortEdgesAntiClockwise()
        {   
            List<Edge> connectedEdgesInOrder = new List<Edge>();
            int edgeIndex = -1;
            if (boundaryVert)//if on a boundary we should find first edge anticlockwise on boundary
            {
                for (int i = 0; i < connectedEdges.Count; i++)
                {
                    Edge edge = connectedEdges[i];
                    if (edge.boundaryEdge)
                    {
                        if (edge.beginVert == this && edge.rightFace == null)
                        {
                            edgeIndex = i;
                            break;
                        }
                        else if (edge.endVert == this && edge.leftFace == null)
                        {
                            edgeIndex = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                edgeIndex = 0;//if not boundary just start at the first one
            }

            connectedEdgesInOrder.Add(connectedEdges[edgeIndex]);
            for (int i = 0; i < connectedEdges.Count-1; i++)
            {
                Edge startEdge = connectedEdges[edgeIndex];
                edgeIndex = findNextAnticlockwiseEdge(startEdge);
                connectedEdgesInOrder.Add(connectedEdges[edgeIndex]);
                if (edgeIndex == -1)
                {
                }
            }
            connectedEdges = connectedEdgesInOrder;
        }

        private int findNextAnticlockwiseEdge(Edge edge)
        {
            if (edge.beginVert == this)
            {
                for (int i = 0; i < connectedEdges.Count; i++)
                {
                    Edge nextEdge = connectedEdges[i];
                    if (nextEdge.beginVert == this)
                    {
                        if (edge.leftFace == nextEdge.rightFace)
                        {
                            return i;
                        }
                    }
                    else
                    {
                        if (nextEdge.endVert == this)
                        {
                            if (edge.leftFace == nextEdge.leftFace)
                            {
                                return i;
                            }
                        }
                    }
                }
            }
            else//edge.endVert must be this.
            {
                for (int i = 0; i < connectedEdges.Count; i++)
                {
                    Edge nextEdge = connectedEdges[i];
                    if (nextEdge.beginVert == this)
                    {
                        if (edge.rightFace == nextEdge.rightFace)
                        {
                            return i;
                        }
                    }
                    else
                    {
                        if (nextEdge.endVert == this)
                        {
                            if (edge.rightFace == nextEdge.leftFace)
                            {
                                return i;
                            }
                        }
                    }
                }
            }
            return -1;
        }

        private void sortFacesAntiClockwise()
        {
            List<Face> facesInOrder = new List<Face>();
            for (int i = 0; i < connectedEdges.Count; i++)
            {
                Edge edge = connectedEdges[i];
                if (i != connectedEdges.Count-1)
                {
                    if (edge.beginVert == this)
                    {
                        facesInOrder.Add(edge.leftFace);
                    }
                    else
                    {
                        facesInOrder.Add(edge.rightFace);
                    }
                }
                else
                {//we are looking at the last edge, this could be a boundary..
                    //more than last edge could be a boundary?? maybe if we split but not at the moment...
                    if (!edge.boundaryEdge)
                    {
                        if (edge.beginVert == this)
                        {
                            facesInOrder.Add(edge.leftFace);
                        }
                        else
                        {
                            facesInOrder.Add(edge.rightFace);
                        }
                    }
                }
            }
            connectedFaces = facesInOrder;
        }

        internal Face returnNextAntiClockwiseFace(Face face0)
        {
            int indexOfNext=-1;
            for (int i = 0; i < connectedFaces.Count; i++)
            {
                if (face0.index == connectedFaces[i].index)
                {
                    indexOfNext = (i + 1) % connectedFaces.Count;
                    break;
                }
            }
            if (indexOfNext == -1)
            {

            }
            return connectedFaces[indexOfNext];
        }

        internal Edge returnNextAntiClockwiseEdge(Edge edge)
        {
            int indexOfNext = -1;
            for (int i = 0; i < connectedEdges.Count; i++)
            {
                if (edge.index == connectedEdges[i].index)
                {
                    indexOfNext = (i + 1) % connectedEdges.Count;
                    break;
                }
            }
            if (indexOfNext == -1)
            {

            }
            return connectedEdges[indexOfNext];
        }
    }
}
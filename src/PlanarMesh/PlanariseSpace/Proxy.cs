using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Giraffe.WingedMeshSpace;

namespace Giraffe.PlanariseSpace
{
    class Proxy
    {
        public Plane rhinoPlane;
        int index;
        public Point3d origin;
        public int seedFaceOIndex;
        public Boolean[] hasBeenPoppedInToThis;
        public List<int> assignedFaces;
        public Mesh proxyAsMesh;
        Partition partition;
        public List<int> cornerVertsForConnectivityMesh;
        public List<int> boundaryVertsInOrder;
        public List<int> cornerVertsInOrder;

        public Proxy(int tIndex, Point3d tPosition, Vector3f tNormal, int tSeedFaceOIndex, int numFacesInMesh, Partition tPartition)//proxy created from initial seed
        {
            index = tIndex;
            rhinoPlane = new Plane(tPosition, tNormal);
            rhinoPlane.Normal.Unitize();
            origin = tPosition;
            seedFaceOIndex = tSeedFaceOIndex;
            hasBeenPoppedInToThis = new bool[numFacesInMesh];
            UsefulFunctions.setAllArrayTo(false,hasBeenPoppedInToThis);
            hasBeenPoppedInToThis[seedFaceOIndex] = true; //set seed to popped
            assignedFaces = new List<int>();
            assignedFaces.Add(seedFaceOIndex);//add this first face
            partition = tPartition;
            if (partition.controller.metricRef == 0)
            {
                partition.addToQueueInOrder(new PoppedFaceInfo(errorMetricEuclidean(seedFaceOIndex), seedFaceOIndex, index));
            }
            else
            {
                partition.addToQueueInOrder(new PoppedFaceInfo(errorMetric21(seedFaceOIndex), seedFaceOIndex, index));
            }
        }

        internal void popFace(int indexToPop)
        {
            Face faceToPop = partition.controller.wingMesh.faces[indexToPop];

            for (int i = 0; i < faceToPop.surroundingFaceIndices.Count; i++)
            {
                if (faceToPop.surroundingFaceIndices[i] != -1)
                {
                    if (!hasBeenPoppedInToThis[faceToPop.surroundingFaceIndices[i]])//check the added face hasn't already been added to priority with ref to this proxy
                    {
                        if (partition.controller.metricRef == 0)
                        {
                            partition.addToQueueInOrder(new PoppedFaceInfo(errorMetricEuclidean(faceToPop.surroundingFaceIndices[i]), faceToPop.surroundingFaceIndices[i], index));
                            hasBeenPoppedInToThis[faceToPop.surroundingFaceIndices[i]] = true;
                        }
                        else
                        {//do other ref - make this an ennumeration if more added..
                            partition.addToQueueInOrder(new PoppedFaceInfo(errorMetric21(faceToPop.surroundingFaceIndices[i]), faceToPop.surroundingFaceIndices[i], index));
                            hasBeenPoppedInToThis[faceToPop.surroundingFaceIndices[i]] = true;
                        }
                    }
                }
            }
        }

        internal double errorMetricEuclidean(int faceRef)
        {
            return partition.controller.wingMesh.faces[faceRef].faceCentre.DistanceTo(origin);
        }

        internal double errorMetric21(int faceRef)
        {
            Vector3d faceNormal = partition.controller.wingMesh.faces[faceRef].faceNormal;
            Vector3d faceMinusProxyNormal = Vector3d.Subtract(faceNormal,rhinoPlane.Normal);
            return faceMinusProxyNormal.SquareLength;
        }

        internal void findNewSeed()
        {
            int newSeedFaceRef = -1;
            double currentMinError = double.MaxValue;
            for (int i = 0; i < assignedFaces.Count; i++)
            {
                double thisError;
                if (partition.controller.metricRef == 0)
                {
                    thisError = errorMetricEuclidean(assignedFaces[i]);
                }
                else
                {
                    thisError = errorMetric21(assignedFaces[i]);
                }
                if (thisError < currentMinError)
                {
                    currentMinError = thisError;
                    newSeedFaceRef = assignedFaces[i];
                }
            }
            seedFaceOIndex = newSeedFaceRef;
        }

        internal void updateBarycentreAndMesh()
        {
            proxyAsMesh = convertProxyToMesh(partition.controller.rhinoMesh);
            updateOriginAndPlane(AreaMassProperties.Compute(proxyAsMesh).Centroid);//this moves away from the plane a bit. Should this be projected back onto it? maybe
        }

        private void updateOriginAndPlane(Point3d newOrigin)
        {
            origin = newOrigin;
            rhinoPlane = new Plane(newOrigin, findAndUpdateNewAverageNormal());
        }

        private Vector3d findAndUpdateNewAverageNormal()//send some of this out to smaller function?
        {
            Vector3d newNormal = new Vector3d(0.0,0.0,0.0);
            double totalArea = AreaMassProperties.Compute(proxyAsMesh).Area;
            for (int i=0; i<assignedFaces.Count; i++) {
                Vector3f normal = partition.controller.rhinoMesh.FaceNormals[assignedFaces[i]];

                Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh();
                for (int j = 0; j < partition.controller.wingMesh.faces[assignedFaces[i]].faceVerts.Count; j++)
                {
                    Vector3f position = partition.controller.wingMesh.faces[assignedFaces[i]].faceVerts[j].position;
                    mesh.Vertices.Add(position.X,position.Y,position.Z);
                }

                if (partition.controller.wingMesh.faces[assignedFaces[i]].faceVerts.Count == 3)
                {
                    mesh.Faces.AddFace(0, 1, 2);
                }
                else if (partition.controller.wingMesh.faces[assignedFaces[i]].faceVerts.Count == 4)
                {
                    mesh.Faces.AddFace(0, 1, 2, 3);
                }

                double area = AreaMassProperties.Compute(mesh).Area;
                mesh.Normals.ComputeNormals();
                Vector3d areaWeightedNormal = Vector3d.Multiply(area/totalArea, mesh.FaceNormals[0]);
                newNormal = Vector3d.Add(newNormal, areaWeightedNormal);
            }
            return Vector3d.Divide(newNormal, assignedFaces.Count);
        }

        internal void resetProxyInfoAndSetRequiredForNextIteration()
        {
            UsefulFunctions.setAllArrayTo(false, hasBeenPoppedInToThis);
            hasBeenPoppedInToThis[seedFaceOIndex] = true;

            assignedFaces.Clear();
            assignedFaces.Add(seedFaceOIndex);

            partition.addToQueueInOrder(new PoppedFaceInfo(errorMetricEuclidean(seedFaceOIndex), seedFaceOIndex, index));
        }

        internal Mesh convertProxyToMesh(Mesh baseMesh)
        {
            Mesh proxyAsMesh = baseMesh.DuplicateMesh();
            Boolean[] shouldFaceBeDeletedFromCopy = new bool[baseMesh.Faces.Count];
            UsefulFunctions.setAllArrayTo(true, shouldFaceBeDeletedFromCopy);
            for (int i = 0; i < assignedFaces.Count; i++)
            {
                shouldFaceBeDeletedFromCopy[assignedFaces[i]] = false;
            }

            List<int> facesToDelete = new List<int>();
            for (int i = 0; i < baseMesh.Faces.Count; i++)
            {
                if (shouldFaceBeDeletedFromCopy[i])
                {
                    facesToDelete.Add(i);
                }
            }
            proxyAsMesh.Faces.DeleteFaces(facesToDelete);
            return proxyAsMesh;
        }

        internal int getNextAntiClockwiseBoundaryVert(int startIndex)
        {
            int nextVertex = -1;
            for (int j = 0; j < partition.controller.wingMesh.vertices[startIndex].connectedEdges.Count; j++)
            {
                Edge edge = partition.controller.wingMesh.vertices[startIndex].connectedEdges[j];
                if (!edge.boundaryEdge)
                {
                    if (edge.beginVert.index == startIndex)
                    {
                        for (int k = 0; k < assignedFaces.Count; k++)
                        {
                            if (edge.leftFace.index == assignedFaces[k])
                            {
                                Boolean otherFaceIsDifferent = true;
                                for (int l = 0; l < assignedFaces.Count; l++)
                                {
                                    if (edge.rightFace.index == assignedFaces[l])
                                    {
                                        otherFaceIsDifferent = false;
                                    }
                                }
                                if (otherFaceIsDifferent)
                                {
                                    nextVertex = edge.endVert.index;
                                    break;
                                }
                            }
                        }
                    }
                    else if (edge.endVert.index == startIndex)//do check if edge pointing in other direction
                    {
                        for (int k = 0; k < assignedFaces.Count; k++)
                        {
                            if (edge.rightFace.index == assignedFaces[k])
                            {
                                Boolean otherFaceIsDifferent = true;
                                for (int l = 0; l < assignedFaces.Count; l++)
                                {
                                    if (edge.leftFace.index == assignedFaces[l])
                                    {
                                        otherFaceIsDifferent = false;
                                    }
                                }
                                if (otherFaceIsDifferent)
                                {
                                    nextVertex = edge.beginVert.index;
                                    break;
                                }
                            }
                        }
                    }
                }
                else//if it is a boundary edge
                {
                    if (edge.beginVert.index == startIndex)
                    {
                        for (int k = 0; k < assignedFaces.Count; k++)
                        {
                            if (edge.leftFace != null)
                            {
                                if (edge.leftFace.index == assignedFaces[k])
                                {
                                    nextVertex = edge.endVert.index;
                                    break;
                                }
                            }
                        }
                    }
                    else if (edge.endVert.index == startIndex)//do check if edge pointing in other direction
                    {
                        for (int k = 0; k < assignedFaces.Count; k++)
                        {
                            if (edge.rightFace != null)
                            {
                                if (edge.rightFace.index == assignedFaces[k])
                                {

                                    nextVertex = edge.beginVert.index;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return nextVertex;
        }

        internal void sortCornerVerts()
        {
            cornerVertsInOrder = new List<int>();

            for (int i = 0; i < boundaryVertsInOrder.Count; i++)
            {
                for (int j = 0; j < cornerVertsForConnectivityMesh.Count; j++)
                {
                    if (boundaryVertsInOrder[i] == cornerVertsForConnectivityMesh[j])
                    {
                        cornerVertsInOrder.Add(cornerVertsForConnectivityMesh[j]);
                    }
                }
            }
        }

        internal int addBoundaryVerts()
        {
            //corner verts in order is two
            int startIndexOfBoundary = -1;
            Boolean startSet = false;
            int endIndexOfBoundary = -1;
            Boolean endSet = false;

            for (int i = 0; i < boundaryVertsInOrder.Count; i++)
            {
                if (cornerVertsForConnectivityMesh[0] == boundaryVertsInOrder[i])
                {
                    startIndexOfBoundary = i;
                    startSet = true;
                }
                else if (cornerVertsForConnectivityMesh[1] == boundaryVertsInOrder[i])
                {
                    endIndexOfBoundary = i;
                    endSet = true;
                }
            }

            if (!startSet || !endSet)
            {//has not set for some reason
            }

            int vertexToAdd = endIndexOfBoundary - startIndexOfBoundary;//does this automatically 'floor' it?

            Point3d pointToAdd = partition.controller.rhinoMesh.TopologyVertices[boundaryVertsInOrder[vertexToAdd]];

            partition.proxyToMesh.addVertex(UsefulFunctions.convertPoint3dToVector(pointToAdd));

            //should this be inserted or added?!
            cornerVertsInOrder.Insert(1, partition.proxyToMesh.vertices.Count - 1);
            return partition.proxyToMesh.vertices.Count - 1;
        }
    }
}

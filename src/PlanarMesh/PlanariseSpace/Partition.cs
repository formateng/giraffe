using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using PlanarMesh.WingedMeshSpace;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System.Drawing;

namespace PlanarMesh.PlanariseSpace
{
    class Partition
    {
        public List<Proxy> proxies;
        public Boolean[] isFaceAssigned;
        public List<PoppedFaceInfo> priorityQueue;
        public PlanarMesher controller;
        public List<int>[] vertexProxyConnectivity;
        public List<int> verticesForMesh;
        public WingedMesh proxyToMesh;
        public List<FlippedEdge> flippedEdgeInfo;

        public Partition(int numFaces, PlanarMesher tController)
        {
            proxies = new List<Proxy>();
            isFaceAssigned = new Boolean[numFaces];
            UsefulFunctions.setAllArrayTo(false, isFaceAssigned);
            priorityQueue = new List<PoppedFaceInfo>();
            controller = tController;
        }

        internal void seedInitialProxies()
        {
            List<Int32> randomIndexes = new List<Int32>();
            for (int i = 0; i < controller.wingMesh.faces.Count; i++)
            {
                randomIndexes.Add(i);
            }
            UsefulFunctions.Shuffle(randomIndexes,1);

            for (int i = 0; i < controller.numPanels; i++)
            {
                proxies.Add(new Proxy(proxies.Count, controller.rhinoMesh.Faces.GetFaceCenter(randomIndexes[i]), controller.rhinoMesh.FaceNormals[randomIndexes[i]], randomIndexes[i], controller.rhinoMesh.Faces.Count, this));
                isFaceAssigned[randomIndexes[i]] = true;
            }
        }

        /// <summary>
        /// Use this method to bypass the Lloyds clustering algorithm when the mesh to be planarised is already defined.
        /// </summary>
        internal void CreateProxiesFromWingedMesh()
        {
            // non-random equivalent of seedInitialProxies (above)
            for (int i = 0; i < controller.numPanels; i++)
            {
                var normal3d = controller.wingMesh.faces[i].faceNormal;
                var normal3f = new Vector3f((float)normal3d.X, (float)normal3d.Y, (float)normal3d.Z);
                proxies.Add(new Proxy(proxies.Count, controller.wingMesh.faces[i].faceCentre, normal3f, i, controller.wingMesh.faces.Count, this));
                isFaceAssigned[i] = true;
            }
        }

        internal void startCluster()
        {
            for (int i = 0; i < proxies.Count; i++)
            {
                Proxy proxy = proxies[i];
                proxy.popFace(proxy.seedFaceOIndex);
            }
        }

        internal void popOutTop()
        {
            PoppedFaceInfo faceToPop = priorityQueue[0];

            if (isFaceAssigned[faceToPop.faceIndex] == false)//checkface hasn't already been assigned somewhere
            {
                if (faceToPop.error < 0.001)
                {
                    controller.errorContainer.Add("error close to zero - should only happen number of times as planes");
                }
                proxies[faceToPop.proxyIndex].assignedFaces.Add(faceToPop.faceIndex);
                isFaceAssigned[faceToPop.faceIndex] = true;
                proxies[faceToPop.proxyIndex].popFace(faceToPop.faceIndex);
            }
            priorityQueue.Remove(faceToPop);
        }

        internal void updatePartitionAndProxies()
        {
            UsefulFunctions.setAllArrayTo(false, isFaceAssigned);
            for (int i = 0; i < proxies.Count; i++)
            {
                Proxy proxy = proxies[i];
                proxy.updateBarycentreAndMesh();
                proxy.findNewSeed();
                proxy.resetProxyInfoAndSetRequiredForNextIteration();
                isFaceAssigned[proxy.seedFaceOIndex] = true;
            }
        }

        internal void addToQueueInOrder(PoppedFaceInfo poppedFaceInfo)//think about tidying this up.
        {
            Boolean addedSomewhere = false;
            if (priorityQueue.Count == 0)//if empty add straight in - no sorting required
            {
                priorityQueue.Add(poppedFaceInfo);
                addedSomewhere = true;
            }
            else
            {
                if (poppedFaceInfo.error > priorityQueue[priorityQueue.Count-1].error)//send straight to the back should save some time!
                {
                    priorityQueue.Add(poppedFaceInfo);
                    addedSomewhere = true;
                }
                else
                {
                    for (int i = 0; i < priorityQueue.Count; i++)
                    {
                        if (poppedFaceInfo.error <= priorityQueue[i].error)
                        {
                            //it has a smaller error so should take its position and move it down and then skip out of the loop
                            priorityQueue.Insert(i, poppedFaceInfo);
                            addedSomewhere = true;
                            break;
                        }
                    }
                }
            }
            if (addedSomewhere == false)
            {
                controller.errorContainer.Add("face was neveer added anywhere! this should never happen!");
            }
        }

        internal void drawProxies(GH_PreviewUtil preview)
        {
            for (int i = 0; i < proxies.Count; i++)
            {
                preview.AddMesh(proxies[i].proxyAsMesh);
                preview.WireColour = UsefulFunctions.returnRandomColour(i);
                preview.Redraw();
                preview.Clear();
            }
        }

        internal void setVertexProxyConnectivity()//this will go through all the proxies and assign them to the vertices that they touch, if they touch three or more then they are a vertex for the connectivity mesh
        {
            vertexProxyConnectivity = new List<int>[controller.wingMesh.vertices.Count];

            for (int i = 0; i < controller.wingMesh.vertices.Count; i++)
            {
                vertexProxyConnectivity[i] = new List<int>();
            }

            for (int i = 0; i < proxies.Count; i++)
            {
                for (int j = 0; j < proxies[i].assignedFaces.Count; j++)
                {
                    int assignedFace = proxies[i].assignedFaces[j];
                    for (int k = 0; k < controller.wingMesh.faces[assignedFace].faceVerts.Count; k++)
                    {
                        //only add if new..
                        Boolean isNew = true;
                        for (int l = 0; l < vertexProxyConnectivity[controller.wingMesh.faces[assignedFace].faceVerts[k].index].Count; l++)
                        {
                            if (vertexProxyConnectivity[controller.wingMesh.faces[assignedFace].faceVerts[k].index][l] == i)
                            {
                                isNew = false;
                                break;
                            }
                        }
                        if (isNew)
                        {
                            vertexProxyConnectivity[controller.wingMesh.faces[assignedFace].faceVerts[k].index].Add(i);
                        }
                    }
                }
            }
        }

        internal void setProxyCornerVerts()
        {

            for (int i = 0; i < proxies.Count; i++)
            {
                proxies[i].cornerVertsForConnectivityMesh = new List<int>();
            }

            for (int i = 0; i < controller.wingMesh.vertices.Count; i++)
            {
                for (int j = 0; j < vertexProxyConnectivity[i].Count; j++)
                {
                    if (controller.wingMesh.vertices[i].boundaryVert)
                    {
                        if (vertexProxyConnectivity[i].Count > 1)
                        {
                            proxies[vertexProxyConnectivity[i][j]].cornerVertsForConnectivityMesh.Add(i);
                        }
                    }
                    else
                    {
                        if (vertexProxyConnectivity[i].Count > 2)
                        {
                            proxies[vertexProxyConnectivity[i][j]].cornerVertsForConnectivityMesh.Add(i);
                        }
                    }

                }
            }
        }

        internal void sortProxyCornerVerts()
        {
            for (int i = 0; i < proxies.Count; i++)
            {
                Proxy proxy = proxies[i];
                List<int> orderedBoundaryVerts = new List<int>();
                orderedBoundaryVerts.Add(proxy.cornerVertsForConnectivityMesh[0]);//add the first one and start from here.
                int nextVertex = proxy.getNextAntiClockwiseBoundaryVert(proxy.cornerVertsForConnectivityMesh[0]);
                while (nextVertex != proxy.cornerVertsForConnectivityMesh[0])
                {
                    orderedBoundaryVerts.Add(nextVertex);
                    nextVertex = proxy.getNextAntiClockwiseBoundaryVert(nextVertex);
                }
                proxy.boundaryVertsInOrder = orderedBoundaryVerts;
                proxy.sortCornerVerts();
            }
        }

        internal void convertProxiesToWingMesh()
        {
            proxyToMesh = new WingedMesh(controller.errorContainer);
            verticesForMesh = new List<int>();
            for (int i = 0; i < controller.wingMesh.vertices.Count; i++)//set array of vertices to add - the index will equal the new index and the value will equal the old one
            {
                {
                    if (controller.wingMesh.vertices[i].boundaryVert)
                    {
                        if (vertexProxyConnectivity[i].Count > 1)
                        {
                            verticesForMesh.Add(i);
                        }
                    }
                    else
                    {
                        if (vertexProxyConnectivity[i].Count > 2)
                        {
                            verticesForMesh.Add(i);
                        }
                    }
                }
            }

            for (int i=0;i<verticesForMesh.Count;i++) {
                proxyToMesh.addVertex(controller.wingMesh.vertices[verticesForMesh[i]].position);
            }

            for (int i = 0; i < proxies.Count; i++)
            {
                List<int> faceVertsInOrder = proxies[i].cornerVertsInOrder;
                List<int> faceVertIndicesForNewMesh = new List<int>();

                if (faceVertsInOrder.Count == 2)
                {
                    //seems to be too many of these happening!
                    verticesForMesh.Add(proxies[i].addBoundaryVerts());
                }

                for (int j = 0; j < faceVertsInOrder.Count; j++)
                {
                    for (int k = 0; k < verticesForMesh.Count; k++)
                    {
                        if (faceVertsInOrder[j] == verticesForMesh[k])
                        {
                            faceVertIndicesForNewMesh.Add(k);
                            break;
                        }
                    }
                }

                int[] arrayOfIndices = new int[faceVertIndicesForNewMesh.Count];

                for (int j = 0; j < faceVertIndicesForNewMesh.Count; j++)
                {
                    arrayOfIndices[j] = faceVertIndicesForNewMesh[j];
                }
                proxyToMesh.addFaceNoNormalsOrCentre(arrayOfIndices);
            }

            proxyToMesh.setSurroundingBoundaryAndOrdering();
        }



        internal void runIntersections()
        {
            for (int i = 0; i < proxyToMesh.vertices.Count; i++)
            {
                //or could I add at the end and wait for it to get here and treat as a three?!?!? I Like this idea (call the vert you keep straight away..
                Vertex vertex = proxyToMesh.vertices[i];

                if (vertex.boundaryVert && vertex.connectedFaces.Count == 2)
                {
                    intersect2BoundaryPlanes(vertex);
                }

                if (vertex.connectedFaces.Count == 3)
                {
                    intersect3PlanesAndUpdateMesh(vertex);
                }

                if (vertex.connectedFaces.Count == 4)
                {
                    intersect4PlanesAndUpdateMesh(vertex);
                }
            }
        }

        private void intersect2BoundaryPlanes(Vertex vertex)
        {
            Line intersectionLine = new Line();
            Boolean intersectionWork = Rhino.Geometry.Intersect.Intersection.PlanePlane(proxies[vertex.connectedFaces[0].index].rhinoPlane, proxies[vertex.connectedFaces[1].index].rhinoPlane, out intersectionLine);

            if (intersectionWork)
            {
                intersectionLine.Extend(1000, 1000);
                Point3d newPositionForVertex = intersectionLine.ClosestPoint(UsefulFunctions.convertVertexToPoint3d(vertex), false);
                vertex.position = UsefulFunctions.convertPoint3dToVector(newPositionForVertex);
            }
        }

        private void intersect3PlanesAndUpdateMesh(Vertex vertex)
        {
            Point3d intersection = new Point3d();
            Boolean intersectionWork = Rhino.Geometry.Intersect.Intersection.PlanePlanePlane(proxies[vertex.connectedFaces[0].index].rhinoPlane, proxies[vertex.connectedFaces[1].index].rhinoPlane, proxies[vertex.connectedFaces[2].index].rhinoPlane, out intersection);

            if (intersectionWork)//can change this to work directly on the vertex!
            {
                vertex.position = UsefulFunctions.convertPoint3dToVector(intersection);
            }
            else
            {
                controller.errorContainer.Add("an intersection failed! two or more planes are probably planar");
            }
        }

        private void intersect4PlanesAndUpdateMesh(Vertex vertex)
        {
            Point3d firstIntersectionPoint = new Point3d();
            Boolean intersectionWork = Rhino.Geometry.Intersect.Intersection.PlanePlanePlane(proxies[vertex.connectedFaces[0].index].rhinoPlane, proxies[vertex.connectedFaces[1].index].rhinoPlane, proxies[vertex.connectedFaces[3].index].rhinoPlane, out firstIntersectionPoint);
            if (intersectionWork)
            {
                proxyToMesh.splitVertexAndRebuild(vertex, firstIntersectionPoint);
            }
            else
            {
                //shift the planes a bit?
                controller.errorContainer.Add("an intersection failed! two or more planes are probably planar");
            }
        }

        internal void indentifyAllFlippedEdges()
        {
            flippedEdgeInfo = new List<FlippedEdge>();
            for (int i = 0; i < proxyToMesh.faces.Count; i++)
            {
                Face face = proxyToMesh.faces[i];
                
                Curve curve = face.convertFaceToPolyLine().ToNurbsCurve();
                CurveIntersections intersections = Intersection.CurveSelf(curve, 0.1);
                if (intersections.Count > 0)
                {
                    controller.errorContainer.Add("Flipped edge check: " +
                        intersections.Count.ToString() + " intersection(s) found");
                    for (int j = 0; j < intersections.Count; j++)
                    {
                        IntersectionEvent intersection = intersections[j];
                        if (intersection.IsPoint)
                        {
                            int refA = (int) Math.Floor(intersection.ParameterA);
                            int refB = (int)Math.Floor(intersection.ParameterB);
                            if (refB == face.faceEdges.Count)
                            {
                                //has rounded up to the end of the curve
                                //is this possible maybe happening in lots of places
                                //not ideal!!!
                                //refB--;
                            }

                            if (face.faceEdges.Count == 4)
                            {//if 4 add both...
                                List<int> refsToAdd = new List<int>();
                                for (int k = 0; k < 4; k++)
                                {
                                    if (k != refA && k != refB)
                                    {
                                        refsToAdd.Add(k);
                                    }
                                }
                                if (refsToAdd.Count != 2)
                                {
                                }
                                addToFlippedEdgeInfo(refsToAdd[0], face, intersection);
                                addToFlippedEdgeInfo(refsToAdd[1], face, intersection);
                            }
                            else
                            {
                                int flippedEdgeIndex;
                                if (refB - refA == 2)
                                {
                                    flippedEdgeIndex = refB - 1;
                                }
                                else if ((refA + face.faceEdges.Count) - refB == 2)
                                {
                                    flippedEdgeIndex = (refB + 1) % face.faceEdges.Count;
                                }
                                else
                                {
                                    flippedEdgeIndex = -1;
                                    //do not have a simple flipped edge, need to do something else
                                }

                                if (flippedEdgeIndex != -1)
                                {
                                    addToFlippedEdgeInfo(flippedEdgeIndex, face, intersection);
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void addToFlippedEdgeInfo(int flippedEdgeIndex, Face face, IntersectionEvent intersection)
        {
            Boolean isNew = true;
            for (int k = 0; k < flippedEdgeInfo.Count; k++)
            {
                if (flippedEdgeInfo[k].edge.index == face.faceEdges[flippedEdgeIndex].index)
                {
                    flippedEdgeInfo[k].addSecondInfo(face, intersection.PointA);
                    isNew = false;
                }
            }
            if (isNew)
            {
                flippedEdgeInfo.Add(new FlippedEdge(face.faceEdges[flippedEdgeIndex], face, intersection.PointA));
            }
        }

        internal void flipEdges()
        {
            for (int i = 0; i < flippedEdgeInfo.Count; i++)
            {
                if (flippedEdgeInfo[i].doubleIntersection)
                {
                    proxyToMesh.flipEdgeAndRebuild(flippedEdgeInfo[i]);
                }
            }
        }
    }
}
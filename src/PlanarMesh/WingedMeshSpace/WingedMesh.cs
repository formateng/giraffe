using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Giraffe.PlanariseSpace;

namespace Giraffe.WingedMeshSpace
{
    public class WingedMesh : RobotGrasshopper.TypeClass.SimpleGooImplementation
    {

        public List<Vertex> vertices;
        public List<Edge> edges;
        public List<Face> faces;

        public List<String> containerForErrors;

        public WingedMesh(List<String> tContainerForErrors)
        {
            containerForErrors = tContainerForErrors;
            vertices = new List<Vertex>();
            edges = new List<Edge>();
            faces = new List<Face>();
        }

        public WingedMesh(List<String> tContainerForErrors , Mesh inputMesh) {
            containerForErrors = tContainerForErrors;
            vertices = new List<Vertex>();
            edges = new List<Edge>();
            faces = new List<Face>();
            addRhinoMeshVertices(inputMesh.TopologyVertices, inputMesh.Normals);//this should be reduced to a single inputMesh argument
            addRhinoMeshFaces(inputMesh);
            setSurroundingBoundaryAndOrdering();
        }

        public WingedMesh(List<String> tContainerForErrors, List<Polyline> inputPolylines)
        {
            containerForErrors = tContainerForErrors;
            vertices = new List<Vertex>();
            edges = new List<Edge>();
            faces = new List<Face>();

            List<int>[] faceVertRefs = new List<int>[inputPolylines.Count];
            addPolylineMeshVertices(inputPolylines, faceVertRefs);
            addPolylineMeshFaces(inputPolylines, faceVertRefs);
            setSurroundingBoundaryAndOrdering();
        }

        private void addPolylineMeshVertices(List<Polyline> inputPolylines, List<int>[] faceVertRefs)
        {
            for (int i = 0; i < inputPolylines.Count; i++)
            {
                if (inputPolylines[i].IsClosed)
                {
                    Point3d[] pointsForPolyline = inputPolylines[i].ToArray();
                    faceVertRefs[i] = new List<int>();
                    for (int j = 0; j < pointsForPolyline.Length-1; j++)
                    {
                        int isNew = isVertexNew(pointsForPolyline[j], 0.01f);

                        if (isNew == -1)
                        {
                            addVertex(UsefulFunctions.convertPoint3dToVector(pointsForPolyline[j]));
                            faceVertRefs[i].Add(vertices.Count - 1);
                        }
                        else
                        {
                            faceVertRefs[i].Add(isNew);
                        }
                    }
                }
                else
                {
                    //polyline not closed - not good!
                }
            }
        }

        private void addPolylineMeshFaces(List<Polyline> inputPolylines, List<int>[] faceVertRefs)
        {
            for (int i = 0; i < inputPolylines.Count; i++)
            {
                int[] indexArray = new int[faceVertRefs[i].Count];
                for (int j = 0; j < faceVertRefs[i].Count; j++)
                {
                    indexArray[j] = faceVertRefs[i][j];
                }
                addFaceNoNormalsOrCentre(indexArray);
            }
            calculateNormals();
        }

        private int isVertexNew(Point3d pointToCheck, float tolerance)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                Vertex vert = vertices[i];
                if (Math.Abs(pointToCheck.X - vert.position.X) < tolerance)
                {
                    if (Math.Abs(pointToCheck.Y - vert.position.Y) < tolerance)
                    {
                        if (Math.Abs(pointToCheck.Z - vert.position.Z) < tolerance)
                        {
                            return i;
                        }
                    }
                }
            }
            return -1;
        }

        public void setSurroundingBoundaryAndOrdering()
        {
            foreach (Face face in faces)
            {
                face.setSurroundingFaces();
            }
            foreach (Vertex vert in vertices)
            {
                vert.setIfBoundaryVert();
                vert.sortEdgesAndFaceAntiClockwise();
            }
        }

        internal void addVertex(Vector3f position)//maybe add with normal?
        {
            vertices.Add(new Vertex(vertices.Count, position));
        }

        internal void addEdge(Vertex vert1, Vertex vert2)
        {
            if (vert1.index != vert2.index)//check not adding an edge between the same vertex (might want to do this check earlier?)
            {
                edges.Add(new Edge(edges.Count, vert1, vert2));
            }
            else
            {
                containerForErrors.Add("attempted to add an edge between the same vertex (or the same index) - this was not done");
            }
        }

        private void addFace(int[] faceIndices, Vector3f faceNormal, Point3f faceCentre)
        {
            List<Vertex> faceVerts = new List<Vertex>();
            foreach (int i in faceIndices)
            {
                faceVerts.Add(vertices[i]);
            }
            faces.Add(new Face(faces.Count,faceVerts,faceNormal,faceCentre,this));
        }

        public void addFaceNoNormalsOrCentre(int[] faceIndices)
        {
            List<Vertex> faceVerts = new List<Vertex>();
            foreach (int i in faceIndices)
            {
                faceVerts.Add(vertices[i]);
            }
            faces.Add(new Face(faces.Count, faceVerts, this));
        }

        private void addRhinoMeshVertices(Rhino.Geometry.Collections.MeshTopologyVertexList meshVertexList, Rhino.Geometry.Collections.MeshVertexNormalList meshVertexNormalList)
        {
            for (int i = 0; i < meshVertexList.Count; i++)
            {
                addVertex(UsefulFunctions.convertPointToVector(meshVertexList[i]));
            }
        }

        private void addRhinoMeshFaces(Mesh inputMesh)
        {
            for (int i = 0; i < inputMesh.Faces.Count; i++)
            {
                int[] faceIndices = inputMesh.Faces.GetTopologicalVertices(i);

                if (inputMesh.Faces[i].IsTriangle)//have to treat differently if a triangle or not!
                {
                    int[] triFaceIndices = new int[3] {faceIndices[0],faceIndices[1],faceIndices[2]};
                    addFace(triFaceIndices, inputMesh.FaceNormals[i],new Point3f((float) inputMesh.Faces.GetFaceCenter(i).X, (float) inputMesh.Faces.GetFaceCenter(i).Y,(float) inputMesh.Faces.GetFaceCenter(i).Z));
                }
                else
                {
                    addFace(faceIndices, inputMesh.FaceNormals[i], new Point3f((float)inputMesh.Faces.GetFaceCenter(i).X, (float)inputMesh.Faces.GetFaceCenter(i).Y, (float)inputMesh.Faces.GetFaceCenter(i).Z));
                }
                
            }
        }

        internal int isEdgeNew(Vertex vert1, Vertex vert2)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                Edge edge = edges[i];
                if (vert1.index == edge.beginVert.index || vert1.index == edge.endVert.index)
                {
                    if (vert2.index == edge.beginVert.index || vert2.index == edge.endVert.index)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public List<List<Point3f>> convertWingedMeshToRhinoPoints()
        {
            List<List<Point3f>> listOfListOfPoints = new List<List<Point3f>>();

            for (int i = 0; i < faces.Count; i++)
            {
                List<Point3f> boundaryPoints = new List<Point3f>();
                listOfListOfPoints.Add(boundaryPoints);
                Face face = faces[i];
                foreach (Vertex faceVertex in face.faceVerts)
                {
                    boundaryPoints.Add(UsefulFunctions.convertVertexToPoint(faceVertex));
                }
            }
            return listOfListOfPoints;
        }

        public List<Polyline> convertWingedMeshToPolylines()
        {
            List<Polyline> polyLineList = new List<Polyline>();
            for (int i = 0; i < faces.Count; i++)
            {
                Face face = faces[i];
                polyLineList.Add(face.convertFaceToPolyLine());
            }
            return polyLineList;
        }

        internal void splitVertexAndRebuild(Vertex vertex, Point3d firstIntersectionPoint)
        {
            if (vertex.boundaryVert)
            {//probably won't have four on a boundary but if this happens we should be notified
            }

            Face f0 = vertex.connectedFaces[0];
            Face f1 = vertex.connectedFaces[1];
            Face f2 = vertex.connectedFaces[2];
            Face f3 = vertex.connectedFaces[3];

            Edge e0 = vertex.connectedEdges[0];
            Edge e1 = vertex.connectedEdges[1];
            Edge e2 = vertex.connectedEdges[2];
            Edge e3 = vertex.connectedEdges[3];

            Vector3f orignalPosition = new Vector3f(vertex.position.X, vertex.position.Y, vertex.position.Z);
            vertex.position = UsefulFunctions.convertPoint3dToVector(firstIntersectionPoint);//move vertex to the position of the intersection

            //create new vertex at the old position and reference as newVertex
            addVertex(orignalPosition);
            Vertex newVertex = vertices[vertices.Count - 1];
            newVertex.boundaryVert = false;

            //create the new edge - remember it will add itself to the vertex in the wrong order and as a edge boundar
            addEdge(vertex, newVertex);
            Edge newEdge = edges[edges.Count - 1];

            //need to remove the two edges (for each vertex) that are no longer connected
            vertex.connectedEdges.Remove(e2);
            vertex.connectedEdges.Remove(e3);

            //and add them to the new vertex.
            newVertex.connectedEdges.Add(e2);
            newVertex.connectedEdges.Add(e3);
            //should check the above both have three edge (maybe keep to the end and check they have three faces too!

            //set newedges right and leftfaces
            newEdge.rightFace = f1;
            newEdge.leftFace = f3;
            newEdge.boundaryEdge = false;

            //need to update e2 and e3 begin and endverts!
            updateBeginEndVert(e2, vertex, newVertex);
            updateBeginEndVert(e3, vertex, newVertex);

            //remove faces from vertex and add all to newVertex (make sure done in same order as the edges...****)
            vertex.connectedFaces.Remove(f2);
            newVertex.connectedFaces.Add(f1);
            newVertex.connectedFaces.Add(f2);
            newVertex.connectedFaces.Add(f3);

            //add newEdge and newVertex into f3 and f1, in order!
            int insertRef = findEdgePositionInList(f1.faceEdges, e2);
            if (insertRef != -1)
            {
                f1.faceEdges.Insert(insertRef + 1, newEdge);
            }

            insertRef = findEdgePositionInList(f3.faceEdges, e0);
            if (insertRef != -1)
            {
                f3.faceEdges.Insert(insertRef + 1, newEdge);
            }

            //for f3 need to find vertex and for f1 need to find e2's vertex that isn't newVertex..
            //remember to +1 onto the ref that is returned!
            if (e2.beginVert == newVertex)
            {
                insertRef = findVertexPositionInList(f1.faceVerts, e2.endVert);
            }
            else
            {
                insertRef = findVertexPositionInList(f1.faceVerts, e2.beginVert);
            }
            if (insertRef != -1)
            {
                f1.faceVerts.Insert(insertRef + 1, newVertex);
            }
            
            insertRef = findVertexPositionInList(f3.faceVerts, vertex);
            if (insertRef != -1)
            {
                f3.faceVerts.Insert(insertRef + 1, newVertex);
            }

            //update f2 to new Vertex instead of vertex!
            insertRef = findVertexPositionInList(f2.faceVerts, vertex);
            f2.faceVerts[insertRef] = newVertex;

            //set surrounding faces, should be in order now as edges should be correct.
            f1.setSurroundingFaces();
            f3.setSurroundingFaces();
        }

        internal void flipEdgeAndRebuild(FlippedEdge flippedEdge)
        {
            Edge edge = flippedEdge.edge;
            //swap the positions
            edge.beginVert.position = UsefulFunctions.convertPoint3dToVector(flippedEdge.rightFaceIntersection);
            edge.endVert.position = UsefulFunctions.convertPoint3dToVector(flippedEdge.leftFaceIntersection);

            //get faces in anticlockwise order starting with rightFace
            Face face0 = edge.rightFace;
            Face face1 = edge.endVert.returnNextAntiClockwiseFace(face0);
            Face face2 = edge.leftFace;
            Face face3 = edge.beginVert.returnNextAntiClockwiseFace(face2);

            Face face2Check = edge.endVert.returnNextAntiClockwiseFace(face1);
            if (face2.index != face2Check.index)
            {
                //problem we do not have the standard situation
            }

            Face face0Check = edge.beginVert.returnNextAntiClockwiseFace(face3);
            if (face0Check.index != face0.index)
            {
                //again do not have the standard situation
            }

            //get edges in same order
            Edge edge0 = edge.endVert.returnNextAntiClockwiseEdge(edge);
            Edge edge1 = edge.endVert.returnNextAntiClockwiseEdge(edge0);
            Edge edge2 = edge.beginVert.returnNextAntiClockwiseEdge(edge);
            Edge edge3 = edge.beginVert.returnNextAntiClockwiseEdge(edge2);


            int insertRef = findEdgePositionInList(edge.endVert.connectedEdges, edge1);
            if (insertRef != -1)
            {
                edge.endVert.connectedEdges.Insert(insertRef + 1, edge2);
                edge.beginVert.connectedEdges.Remove(edge2);
            }
            else
            {
            }

            insertRef = findEdgePositionInList(edge.beginVert.connectedEdges, edge3);
            if (insertRef != -1)
            {
                edge.beginVert.connectedEdges.Insert(insertRef + 1, edge0);
                edge.endVert.connectedEdges.Remove(edge0);
            }
            else
            {
            }

            insertRef = findVertexPositionInList(face1.faceVerts, edge.endVert);
            if (insertRef != -1)
            {
                face1.faceVerts.Insert(insertRef + 1, edge.beginVert);
            }

            insertRef = findVertexPositionInList(face3.faceVerts, edge.beginVert);
            if (insertRef != -1)
            {
                face3.faceVerts.Insert(insertRef + 1, edge.endVert);
            }

            insertRef = findEdgePositionInList(face1.faceEdges, edge1);
            if (insertRef != -1)
            {
                face1.faceEdges.Insert(insertRef + 1, edge);
            }
            insertRef = findEdgePositionInList(face3.faceEdges, edge3);
            if (insertRef != -1)
            {
                face3.faceEdges.Insert(insertRef + 1, edge);
            }

            face0.faceEdges.Remove(edge);
            face2.faceEdges.Remove(edge);
            face0.faceVerts.Remove(edge.endVert);
            face2.faceVerts.Remove(edge.beginVert);

            //is there something going wrong here!
            //at some point
            if (edge0.beginVert == edge.endVert)
            {
                edge0.beginVert = edge.beginVert;
            }
            else
            {
                edge0.endVert = edge.beginVert;
            }
            if (edge2.beginVert == edge.beginVert)
            {
                edge2.beginVert = edge.endVert;
            }
            else
            {
                edge2.endVert = edge.endVert;
            }

            edge.rightFace = face1;
            edge.leftFace = face3;

        }

        private int findVertexPositionInList(List<Vertex> list, Vertex vertexToLocate)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == vertexToLocate)
                {
                    return i;
                }
            }
            return -1;
        }

        private void updateBeginEndVert(Edge edge, Vertex vertex, Vertex newVertex)
        {
            if (edge.beginVert == vertex)
            {
                edge.beginVert = newVertex;
            }
            else
            {
                edge.endVert = newVertex;
            }
        }

        internal int findEdgePositionInList(List<Edge> listOfEdges, Edge edgeToLocate)
        {
            for (int i = 0; i < listOfEdges.Count; i++)
            {
                if (listOfEdges[i] == edgeToLocate)
                {
                    return i;
                }
            }
            return -1;
        }

        internal void calculateNormals()
        {
            //calculate the normals
            for (int i = 0; i < faces.Count; i++)
            {
                faces[i].calculateFaceNormal();
            }
        }
    }
}

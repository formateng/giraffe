using System;
using System.Collections.Generic;
using Rhino.Geometry;
using PlanarMesh.WingedMeshSpace;
using System.Drawing;

namespace PlanarMesh
{
    class UsefulFunctions
    {
        public static Vector3f convertPointToVector(Point3f pointToConvert)
        {
            return new Vector3f(pointToConvert.X, pointToConvert.Y, pointToConvert.Z);
        }

        public static Point3f convertVertexToPoint(Vertex vertexToConvert)
        {
            return new Point3f((float) vertexToConvert.position.X, (float) vertexToConvert.position.Y, (float) vertexToConvert.position.Z);
        }

        public static Point3d convertVertexToPoint3d(Vertex vertexToConvert)
        {
            return new Point3d(vertexToConvert.position.X, vertexToConvert.position.Y, vertexToConvert.position.Z);
        }

        internal static Vector3d convertVertexToVector3d(Vertex vertexToConvert)
        {
            return new Vector3d(vertexToConvert.position.X, vertexToConvert.position.Y, vertexToConvert.position.Z);
        }

        internal static Vector3f convertPoint3dToVector(Point3d pointToConvert)
        {
            return new Vector3f((float) pointToConvert.X, (float) pointToConvert.Y, (float) pointToConvert.Z);
        }

        public static void Shuffle(List<int> list, int tSeed) //shuffle a list of integers, if this is an array on zero based increasing integers then this is a good way to get random but unique items from a list
        {
            Random rng = new Random(tSeed);
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                int value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static void Shuffle(List<int> list) //shuffle but random each time..
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                int value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static Color returnRandomColour(int seed)
        {
            Random rng = new Random(seed);
            return Color.FromArgb(rng.Next(255), rng.Next(255), rng.Next(255));
        }

        public static void setAllArrayTo(Boolean tVal, Boolean[] arrayToSet)
        {
            for (int i = 0; i < arrayToSet.Length; i++)
            {
                arrayToSet[i] = tVal;
            }
        }
    }
}

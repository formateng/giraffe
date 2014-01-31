using System;

using NUnit.Framework;
using PlanarMesh.WingedMeshSpace;
using Rhino.Geometry;


namespace PlanarMeshTest.WingedMeshSpace
{
    [TestFixture]
    public class VertexTests
    {
     
        Vertex SUT;

        [SetUp]
        public void Setup()
        {
            SUT = new Vertex(0, new Vector3f(0.0f,10.0f,0.0f));
        }

        [Test]
        public void CanDoSimpleTest()
        {
            Assert.Ignore("Test not yet implemented");
        }

        [TearDown]
        public void TearDown()
        {
            SUT = null;
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LightJsonTests.ToonTests.Encode
{
    [TestClass]
    public class ArraysObjectsTests : ToonTestBase
    {
        [TestMethod]
        public void TestArraysObjects()
        {
            RunEncodeTests("arrays-objects.json");
        }
    }
}

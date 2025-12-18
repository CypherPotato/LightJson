using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LightJsonTests.ToonTests.Encode
{
    [TestClass]
    public class ArraysNestedTests : ToonTestBase
    {
        [TestMethod]
        public void TestArraysNested()
        {
            RunEncodeTests("arrays-nested.json");
        }
    }
}

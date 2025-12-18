using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LightJsonTests.ToonTests.Encode
{
    [TestClass]
    public class ArraysTabularTests : ToonTestBase
    {
        [TestMethod]
        public void TestArraysTabular()
        {
            RunEncodeTests("arrays-tabular.json");
        }
    }
}

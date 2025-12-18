using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LightJsonTests.ToonTests.Encode
{
    [TestClass]
    public class PrimitivesTests : ToonTestBase
    {
        [TestMethod]
        public void TestPrimitives()
        {
            RunEncodeTests("primitives.json");
        }
    }
}

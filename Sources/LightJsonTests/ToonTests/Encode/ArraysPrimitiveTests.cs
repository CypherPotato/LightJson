using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LightJsonTests.ToonTests.Encode
{
    [TestClass]
    public class ArraysPrimitiveTests : ToonTestBase
    {
        [TestMethod]
        public void TestArraysPrimitive()
        {
            RunEncodeTests("arrays-primitive.json");
        }
    }
}

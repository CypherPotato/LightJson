using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LightJsonTests.ToonTests.Encode
{
    [TestClass]
    public class ObjectsTests : ToonTestBase
    {
        [TestMethod]
        public void TestObjects()
        {
            RunEncodeTests("objects.json");
        }
    }
}

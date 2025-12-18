using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LightJsonTests.ToonTests.Encode
{
    [TestClass]
    public class KeyFoldingTests : ToonTestBase
    {
        [TestMethod]
        public void TestKeyFolding()
        {
            RunEncodeTests("key-folding.json");
        }
    }
}

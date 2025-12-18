using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LightJsonTests.ToonTests.Encode
{
    [TestClass]
    public class WhitespaceTests : ToonTestBase
    {
        [TestMethod]
        public void TestWhitespace()
        {
            RunEncodeTests("whitespace.json");
        }
    }
}

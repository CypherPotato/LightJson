using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LightJsonTests.ToonTests.Encode
{
    [TestClass]
    public class DelimitersTests : ToonTestBase
    {
        [TestMethod]
        public void TestDelimiters()
        {
            RunEncodeTests("delimiters.json");
        }
    }
}

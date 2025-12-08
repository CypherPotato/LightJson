using LightJson;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LightJsonTests
{
    [TestClass]
    public class BasicSerializationTests
    {
        JsonOptions testsOptions = new JsonOptions();

        [TestMethod]
        public void ParseExampleMessage()
        {
            var message = @"
				{
					""menu"": [
						""home"",
						""projects"",
						""about""
					]
				}
			";

            var json = testsOptions.Deserialize(message);

            Assert.IsTrue(json.IsJsonObject);

            Assert.AreEqual(1, json.GetJsonObject().Count);
            Assert.IsTrue(json.GetJsonObject().ContainsKey("menu"));

            var items = json["menu"].GetJsonArray();

            Assert.IsNotNull(items);
            Assert.AreEqual(3, items.Count);
            Assert.IsTrue(items.Contains("home"));
            Assert.IsTrue(items.Contains("projects"));
            Assert.IsTrue(items.Contains("about"));
        }

        [TestMethod]
        public void SerializeExampleMessage()
        {
            var json = new JsonObject
            {
                ["menu"] = new JsonArray
                {
                    "home",
                    "projects",
                    "about",
                }
            };

            var message = json.ToString(testsOptions);
            var expectedMessage = @"{""menu"":[""home"",""projects"",""about""]}";

            Assert.AreEqual(expectedMessage, message);
        }

        private class ClassWithPrivateProperties
        {
            public string PublicProp { get; set; } = "Public";
            private string PrivateProp { get; set; } = "Private";
        }

        [TestMethod]
        public void Serialize_IgnoresPrivateProperties()
        {
            var obj = new ClassWithPrivateProperties();
            var json = JsonValue.Serialize(obj);

            Assert.IsTrue(json.IsJsonObject);
            Assert.IsTrue(json.GetJsonObject().ContainsKey("PublicProp"));
            Assert.IsFalse(json.GetJsonObject().ContainsKey("PrivateProp"));
        }
    }
}

using LightJson;
using LightJson.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LightJsonTests
{
    [TestClass]
    public class PropertyNameComparerTests
    {
        [TestMethod]
        public void PropertyNameComparer_Ordinal_IsCaseSensitive()
        {
            var options = new JsonOptions { PropertyNameComparer = StringComparer.Ordinal };
            var obj = new JsonObject(options);
            obj["Name"] = "Value";

            Assert.IsTrue(obj.ContainsKey("Name"));
            Assert.IsFalse(obj.ContainsKey("name"));
            Assert.AreEqual(JsonValueType.Undefined, obj["name"].Type);
        }

        [TestMethod]
        public void PropertyNameComparer_OrdinalIgnoreCase_IsCaseInsensitive()
        {
            var options = new JsonOptions { PropertyNameComparer = StringComparer.OrdinalIgnoreCase };
            var obj = new JsonObject(options);
            obj["Name"] = "Value";

            Assert.IsTrue(obj.ContainsKey("Name"));
            Assert.IsTrue(obj.ContainsKey("name"));
            Assert.AreEqual("Value", obj["name"].GetString());
        }

        [TestMethod]
        public void PropertyNameComparer_Default_UsesOrdinal()
        {
            var obj = new JsonObject(); // Uses JsonOptions.Default
            obj["Name"] = "Value";

            Assert.IsFalse(obj.ContainsKey("name"));
        }

        [TestMethod]
        public void PropertyNameComparer_Serialize_PreservesOriginalCasing()
        {
            var options = new JsonOptions { PropertyNameComparer = StringComparer.OrdinalIgnoreCase };
            var obj = new JsonObject(options);
            obj["Name"] = "Value";

            var json = obj.ToString();
            Assert.IsTrue(json.Contains("\"Name\""));
        }

        [TestMethod]
        public void PropertyNameComparer_Deserialize_UsesComparerForDuplicates()
        {
            var json = "{\"Name\": \"Value1\", \"name\": \"Value2\"}";
            var options = new JsonOptions
            {
                PropertyNameComparer = StringComparer.OrdinalIgnoreCase,
                ThrowOnDuplicateObjectKeys = true
            };

            Assert.ThrowsException<JsonParseException>(() => options.Deserialize(json));
        }

        [TestMethod]
        public void PropertyNameComparer_ContainsKey_RespectsComparer()
        {
            var options = new JsonOptions { PropertyNameComparer = StringComparer.OrdinalIgnoreCase };
            var obj = new JsonObject(options);
            obj["Name"] = "Value";

            Assert.IsTrue(obj.ContainsKey("NAME"));
        }
    }
}

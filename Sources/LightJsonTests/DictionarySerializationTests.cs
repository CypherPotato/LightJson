using LightJson;
using LightJson.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace LightJsonTests
{
    [TestClass]
    public class DictionarySerializationTests
    {
        [TestMethod]
        public void Serialize_StringKeyDictionary_SerializesAsObject()
        {
            var dict = new Dictionary<string, int> { ["One"] = 1, ["Two"] = 2 };
            var jsonVal = JsonValue.Serialize(dict);
            Assert.AreEqual(1, jsonVal["One"].GetInteger());
            Assert.AreEqual(2, jsonVal["Two"].GetInteger());
        }

        [TestMethod]
        public void Deserialize_JsonObject_ToDictionary()
        {
            var json = "{\"One\": 1, \"Two\": 2}";
            var dict = JsonValue.Parse(json, null).Get<Dictionary<string, int>>();
            Assert.AreEqual(1, dict["One"]);
            Assert.AreEqual(2, dict["Two"]);
        }

        [TestMethod]
        public void Dictionary_RoundTrip_PreservesData()
        {
            var dict = new Dictionary<string, int> { ["One"] = 1, ["Two"] = 2 };
            var json = JsonValue.Serialize(dict).ToString();
            var deserialized = JsonValue.Parse(json, null).Get<Dictionary<string, int>>();
            CollectionAssert.AreEqual(dict, deserialized);
        }

        [TestMethod]
        public void Serialize_IntKeyDictionary_SerializesWithStringKeys()
        {
            var dict = new Dictionary<int, string> { [1] = "One", [2] = "Two" };
            var jsonVal = JsonValue.Serialize(dict);
            Assert.AreEqual("One", jsonVal["1"].GetString());
            Assert.AreEqual("Two", jsonVal["2"].GetString());
        }

        [TestMethod]
        public void Deserialize_ToIntKeyDictionary_ConvertsKeys()
        {
            var json = "{\"1\": \"One\", \"2\": \"Two\"}";
            var dict = JsonValue.Parse(json, null).Get<Dictionary<int, string>>();
            Assert.AreEqual("One", dict[1]);
            Assert.AreEqual("Two", dict[2]);
        }

        [TestMethod]
        public void Serialize_NestedDictionary_SerializesRecursively()
        {
            var dict = new Dictionary<string, Dictionary<string, int>>
            {
                ["Root"] = new Dictionary<string, int> { ["Child"] = 1 }
            };
            var jsonVal = JsonValue.Serialize(dict);
            Assert.AreEqual(1, jsonVal["Root"]["Child"].GetInteger());
        }

        [TestMethod]
        public void Serialize_EmptyDictionary_ProducesEmptyObject()
        {
            var dict = new Dictionary<string, int>();
            var json = JsonValue.Serialize(dict).ToString();
            Assert.AreEqual("{}", json);
        }

        [TestMethod]
        public void Deserialize_EmptyObject_ProducesEmptyDictionary()
        {
            var json = "{}";
            var dict = JsonValue.Parse(json, null).Get<Dictionary<string, int>>();
            Assert.AreEqual(0, dict.Count);
        }
    }
}

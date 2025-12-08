using LightJson;
using LightJson.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LightJsonTests
{
    [TestClass]
    public class JsonObjectSerializationTests
    {
        [TestMethod]
        public void Serialize_EmptyObject_ReturnsEmptyJsonString()
        {
            var obj = new JsonObject();
            var json = obj.ToString();
            Assert.AreEqual("{}", json);
        }

        [TestMethod]
        public void Serialize_SimpleProperties_ReturnsCorrectJson()
        {
            var obj = new JsonObject
            {
                ["string"] = "value",
                ["number"] = 42,
                ["boolean"] = true
            };
            var json = obj.ToString();
            // Note: Order is not guaranteed in dictionary, but usually insertion order or hash order.
            // LightJson JsonObject might preserve order or not.
            // Assuming standard JSON serialization.
            Assert.IsTrue(json.Contains("\"string\":\"value\""));
            Assert.IsTrue(json.Contains("\"number\":42"));
            Assert.IsTrue(json.Contains("\"boolean\":true"));
        }

        [TestMethod]
        public void Serialize_NestedObjects_ReturnsCorrectJson()
        {
            var obj = new JsonObject
            {
                ["nested"] = new JsonObject
                {
                    ["inner"] = "value"
                }
            };
            var json = obj.ToString();
            Assert.IsTrue(json.Contains("\"nested\":{\"inner\":\"value\"}"));
        }

        [TestMethod]
        public void Serialize_WithIndentation_ReturnsFormattedJson()
        {
            var obj = new JsonObject { ["key"] = "value" };
            var options = new JsonOptions { WriteIndented = true };
            var json = obj.ToString(options);
            // Check for newlines or indentation
            Assert.IsTrue(json.Contains("\n") || json.Contains("\r\n"));
            Assert.IsTrue(json.Contains("  ") || json.Contains("\t"));
        }

        [TestMethod]
        public void Deserialize_ValidJsonString_ReturnsJsonObject()
        {
            var json = "{\"key\":\"value\"}";
            var obj = JsonValue.Parse(json, null).GetJsonObject();
            Assert.AreEqual("value", obj["key"].GetString());
        }

        [TestMethod]
        public void Deserialize_NestedObjects_ReturnsCorrectStructure()
        {
            var json = "{\"nested\":{\"inner\":\"value\"}}";
            var obj = JsonValue.Parse(json, null).GetJsonObject();
            Assert.AreEqual("value", obj["nested"].GetJsonObject()["inner"].GetString());
        }

        [TestMethod]
        public void Deserialize_WithUnicodeCharacters_HandlesCorrectly()
        {
            var json = "{\"key\":\"\\u00A9\"}"; // Copyright symbol
            var obj = JsonValue.Parse(json, null).GetJsonObject();
            Assert.AreEqual("\u00A9", obj["key"].GetString());
        }

        [TestMethod]
        public void Deserialize_WithEscapedCharacters_HandlesCorrectly()
        {
            var json = "{\"key\":\"\\\"\\\\\"}"; // "\""
            var obj = JsonValue.Parse(json, null).GetJsonObject();
            Assert.AreEqual("\"\\", obj["key"].GetString());
        }

        [TestMethod]
        public void RoundTrip_ComplexObject_PreservesData()
        {
            var original = new JsonObject
            {
                ["str"] = "test",
                ["num"] = 123,
                ["bool"] = false,
                ["null"] = JsonValue.Null,
                ["nested"] = new JsonObject { ["a"] = 1 }
            };
            var json = original.ToString();
            var deserialized = JsonValue.Parse(json, null).GetJsonObject();

            Assert.AreEqual("test", deserialized["str"].GetString());
            Assert.AreEqual(123, deserialized["num"].GetInteger());
            Assert.IsFalse(deserialized["bool"].GetBoolean());
            Assert.IsTrue(deserialized["null"].IsNull);
            Assert.AreEqual(1, deserialized["nested"].GetJsonObject()["a"].GetInteger());
        }

        [TestMethod]
        public void Serialize_WithNullValues_IncludesNullProperties()
        {
            var obj = new JsonObject { ["key"] = JsonValue.Null };
            var json = obj.ToString();
            Assert.IsTrue(json.Contains("\"key\":null"));
        }

        [TestMethod]
        public void Deserialize_WithDuplicateKeys_BehavesAccordingToOptions()
        {
            var json = "{\"key\":\"value1\", \"key\":\"value2\"}";

            // Default behavior (ThrowOnDuplicateObjectKeys = false)
            var obj = JsonValue.Parse(json, null).GetJsonObject();
            Assert.AreEqual("value2", obj["key"].GetString());

            // Throw behavior
            var options = new JsonOptions { ThrowOnDuplicateObjectKeys = true };
            Assert.ThrowsException<JsonParseException>(() => options.Deserialize(json));
        }

        [TestMethod]
        public void Serialize_LargeObject_HandlesCorrectly()
        {
            var obj = new JsonObject();
            for (int i = 0; i < 1000; i++)
            {
                obj["key" + i] = i;
            }
            var json = obj.ToString();
            var deserialized = JsonValue.Parse(json, null).GetJsonObject();
            Assert.AreEqual(1000, deserialized.Count);
            Assert.AreEqual(999, deserialized["key999"].GetInteger());
        }

        [TestMethod]
        public void Deserialize_EmptyObject_ReturnsEmptyJsonObject()
        {
            var json = "{}";
            var obj = JsonValue.Parse(json, null).GetJsonObject();
            Assert.AreEqual(0, obj.Count);
        }

        [TestMethod]
        public void Serialize_WithSpecialCharacters_EscapesCorrectly()
        {
            var obj = new JsonObject { ["key"] = "\"\n\t\\" };
            var json = obj.ToString();
            // Expected: "key":"\"\n\t\\"
            // Escaped in C# string: "\"\\\"\\n\\t\\\\\""
            Assert.IsTrue(json.Contains("\\\"\\n\\t\\\\"));
        }

        [TestMethod]
        public void Deserialize_WithComments_BehavesAccordingToFlags()
        {
            var json = "{/* comment */ \"key\":\"value\"}";

            // Default behavior (IgnoreComments = false) - Should fail
            Assert.ThrowsException<JsonParseException>(() => JsonValue.Parse(json, null));

            // IgnoreComments = true
            var options = new JsonOptions { SerializationFlags = JsonSerializationFlags.IgnoreComments };
            var obj = options.Deserialize(json).GetJsonObject();
            Assert.AreEqual("value", obj["key"].GetString());
        }
    }
}

using LightJson;
using LightJson.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LightJsonTests
{
    [TestClass]
    public class JsonArraySerializationTests
    {
        [TestMethod]
        public void Serialize_EmptyArray_ReturnsEmptyJsonArray()
        {
            var arr = new JsonArray();
            var json = arr.ToString();
            Assert.AreEqual("[]", json);
        }

        [TestMethod]
        public void Serialize_PrimitiveElements_ReturnsCorrectJson()
        {
            var arr = new JsonArray { "string", 42, true };
            var json = arr.ToString();
            Assert.AreEqual("[\"string\",42,true]", json);
        }

        [TestMethod]
        public void Serialize_MixedTypes_ReturnsCorrectJson()
        {
            var arr = new JsonArray
            {
                "string",
                42,
                true,
                new JsonObject { ["key"] = "value" },
                new JsonArray { 1, 2 }
            };
            var json = arr.ToString();
            Assert.AreEqual("[\"string\",42,true,{\"key\":\"value\"},[1,2]]", json);
        }

        [TestMethod]
        public void Serialize_NestedArrays_ReturnsCorrectJson()
        {
            var arr = new JsonArray
            {
                new JsonArray { 1, 2 },
                new JsonArray { 3, 4 }
            };
            var json = arr.ToString();
            Assert.AreEqual("[[1,2],[3,4]]", json);
        }

        [TestMethod]
        public void Serialize_WithIndentation_ReturnsFormattedJson()
        {
            var arr = new JsonArray { 1, 2 };
            var options = new JsonOptions { WriteIndented = true };
            var json = arr.ToString(options);
            Assert.IsTrue(json.Contains("\n") || json.Contains("\r\n"));
            Assert.IsTrue(json.Contains("  ") || json.Contains("\t"));
        }

        [TestMethod]
        public void Deserialize_ValidJsonArray_ReturnsJsonArray()
        {
            var json = "[1, 2, 3]";
            var arr = JsonValue.Parse(json, null).GetJsonArray();
            Assert.AreEqual(3, arr.Count);
            Assert.AreEqual(1, arr[0].GetInteger());
            Assert.AreEqual(2, arr[1].GetInteger());
            Assert.AreEqual(3, arr[2].GetInteger());
        }

        [TestMethod]
        public void Deserialize_NestedArrays_ReturnsCorrectStructure()
        {
            var json = "[[1, 2], [3, 4]]";
            var arr = JsonValue.Parse(json, null).GetJsonArray();
            Assert.AreEqual(2, arr.Count);
            Assert.AreEqual(1, arr[0].GetJsonArray()[0].GetInteger());
            Assert.AreEqual(3, arr[1].GetJsonArray()[0].GetInteger());
        }

        [TestMethod]
        public void Deserialize_MixedTypes_PreservesTypes()
        {
            var json = "[\"string\", 42, true, null, {}, []]";
            var arr = JsonValue.Parse(json, null).GetJsonArray();
            Assert.IsTrue(arr[0].IsString);
            Assert.IsTrue(arr[1].IsInteger);
            Assert.IsTrue(arr[2].IsBoolean);
            Assert.IsTrue(arr[3].IsNull);
            Assert.IsTrue(arr[4].IsJsonObject);
            Assert.IsTrue(arr[5].IsJsonArray);
        }

        [TestMethod]
        public void RoundTrip_ComplexArray_PreservesData()
        {
            var original = new JsonArray
            {
                "test",
                123,
                false,
                JsonValue.Null,
                new JsonObject { ["a"] = 1 },
                new JsonArray { 2, 3 }
            };
            var json = original.ToString();
            var deserialized = JsonValue.Parse(json, null).GetJsonArray();

            Assert.AreEqual("test", deserialized[0].GetString());
            Assert.AreEqual(123, deserialized[1].GetInteger());
            Assert.IsFalse(deserialized[2].GetBoolean());
            Assert.IsTrue(deserialized[3].IsNull);
            Assert.AreEqual(1, deserialized[4].GetJsonObject()["a"].GetInteger());
            Assert.AreEqual(2, deserialized[5].GetJsonArray()[0].GetInteger());
        }

        [TestMethod]
        public void Serialize_ArrayOfObjects_ReturnsCorrectJson()
        {
            var arr = new JsonArray
            {
                new JsonObject { ["id"] = 1 },
                new JsonObject { ["id"] = 2 }
            };
            var json = arr.ToString();
            Assert.AreEqual("[{\"id\":1},{\"id\":2}]", json);
        }

        [TestMethod]
        public void Deserialize_LargeArray_HandlesCorrectly()
        {
            var arr = new JsonArray();
            for (int i = 0; i < 10000; i++)
            {
                arr.Add(i);
            }
            var json = arr.ToString();
            var deserialized = JsonValue.Parse(json, null).GetJsonArray();
            Assert.AreEqual(10000, deserialized.Count);
            Assert.AreEqual(9999, deserialized[9999].GetInteger());
        }

        [TestMethod]
        public void Serialize_WithNullElements_IncludesNulls()
        {
            var arr = new JsonArray { JsonValue.Null, JsonValue.Null };
            var json = arr.ToString();
            Assert.AreEqual("[null,null]", json);
        }

        [TestMethod]
        public void Deserialize_EmptyArray_ReturnsEmptyJsonArray()
        {
            var json = "[]";
            var arr = JsonValue.Parse(json, null).GetJsonArray();
            Assert.AreEqual(0, arr.Count);
        }

        [TestMethod]
        public void Serialize_ArrayWithUnicodeStrings_HandlesCorrectly()
        {
            var arr = new JsonArray { "\u00A9", "ñ" };
            var json = arr.ToString();
            // Assuming default encoding/escaping
            // \u00A9 might be serialized as "\u00A9" or literal depending on encoder.
            // LightJson usually escapes non-ascii if not configured otherwise?
            // Let's check if it contains the characters or escapes.
            // Based on JsonObject tests, it seems to handle unicode.
            // But let's be safe and check deserialization or partial match.
            var deserialized = JsonValue.Parse(json, null).GetJsonArray();
            Assert.AreEqual("\u00A9", deserialized[0].GetString());
            Assert.AreEqual("ñ", deserialized[1].GetString());
        }

        [TestMethod]
        public void Deserialize_WithTrailingComma_BehavesAccordingToFlags()
        {
            var json = "[1, 2, ]";

            // Default behavior (IgnoreTrailingComma = false) - Should fail
            Assert.ThrowsException<JsonParseException>(() => JsonValue.Parse(json, null));

            // IgnoreTrailingComma = true
            var options = new JsonOptions { SerializationFlags = JsonSerializationFlags.IgnoreTrailingComma };
            var arr = options.Deserialize(json).GetJsonArray();
            Assert.AreEqual(2, arr.Count);
            Assert.AreEqual(2, arr[1].GetInteger());
        }
    }
}

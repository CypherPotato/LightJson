using LightJson;
using LightJson.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LightJsonTests
{
    [TestClass]
    public class Json5SerializationTests
    {
        [TestMethod]
        public void Deserialize_WithSingleLineComments_ParsesCorrectly()
        {
            var json = "{ // comment\n \"key\": \"value\" }";
            var options = new JsonOptions { SerializationFlags = JsonSerializationFlags.IgnoreComments };
            var obj = options.Deserialize(json).GetJsonObject();
            Assert.AreEqual("value", obj["key"].GetString());
        }

        [TestMethod]
        public void Deserialize_WithMultiLineComments_ParsesCorrectly()
        {
            var json = "{ /* comment */ \"key\": \"value\" }";
            var options = new JsonOptions { SerializationFlags = JsonSerializationFlags.IgnoreComments };
            var obj = options.Deserialize(json).GetJsonObject();
            Assert.AreEqual("value", obj["key"].GetString());
        }

        [TestMethod]
        public void Deserialize_WithTrailingCommasInObject_ParsesCorrectly()
        {
            var json = "{ \"key\": \"value\", }";
            var options = new JsonOptions { SerializationFlags = JsonSerializationFlags.IgnoreTrailingComma };
            var obj = options.Deserialize(json).GetJsonObject();
            Assert.AreEqual("value", obj["key"].GetString());
        }

        [TestMethod]
        public void Deserialize_WithTrailingCommasInArray_ParsesCorrectly()
        {
            var json = "[ 1, 2, ]";
            var options = new JsonOptions { SerializationFlags = JsonSerializationFlags.IgnoreTrailingComma };
            var arr = options.Deserialize(json).GetJsonArray();
            Assert.AreEqual(2, arr.Count);
        }

        [TestMethod]
        public void Deserialize_WithUnquotedKeys_ParsesCorrectly()
        {
            var json = "{ key: \"value\" }";
            var options = new JsonOptions { SerializationFlags = JsonSerializationFlags.AllowUnquotedPropertyNames };
            var obj = options.Deserialize(json).GetJsonObject();
            Assert.AreEqual("value", obj["key"].GetString());
        }

        [TestMethod]
        public void Deserialize_WithSingleQuotedStrings_ParsesCorrectly()
        {
            var json = "{ 'key': 'value' }";
            var options = new JsonOptions { SerializationFlags = JsonSerializationFlags.AllowSingleQuotes | JsonSerializationFlags.AllowUnquotedPropertyNames };
            // Note: 'key' is a string, so it might not need AllowUnquotedPropertyNames if it's quoted with single quotes.
            // But usually standard JSON requires double quotes.
            // Let's try with just AllowSingleQuotes.
            options.SerializationFlags = JsonSerializationFlags.AllowSingleQuotes;
            // Wait, if the key is single quoted, it's a string.
            var obj = options.Deserialize(json).GetJsonObject();
            Assert.AreEqual("value", obj["key"].GetString());
        }

        [TestMethod]
        public void Deserialize_WithHexadecimalNumbers_ParsesCorrectly()
        {
            var json = "{ \"key\": 0xFF }";
            var options = new JsonOptions { SerializationFlags = JsonSerializationFlags.HexadecimalNumberLiterals };
            var obj = options.Deserialize(json).GetJsonObject();
            Assert.AreEqual(255, obj["key"].GetInteger());
        }

        [TestMethod]
        public void Deserialize_WithLeadingDecimalPoint_ParsesCorrectly()
        {
            var json = "{ \"key\": .5 }";
            var options = new JsonOptions { SerializationFlags = JsonSerializationFlags.LeadingDecimalPoint };
            var obj = options.Deserialize(json).GetJsonObject();
            Assert.AreEqual(0.5, obj["key"].GetNumber());
        }

        [TestMethod]
        public void Deserialize_WithTrailingDecimalPoint_ParsesCorrectly()
        {
            var json = "{ \"key\": 5. }";
            var options = new JsonOptions { SerializationFlags = JsonSerializationFlags.TrailingDecimalPoint };
            var obj = options.Deserialize(json).GetJsonObject();
            Assert.AreEqual(5.0, obj["key"].GetNumber());
        }

        [TestMethod]
        public void Deserialize_WithPositiveSign_ParsesCorrectly()
        {
            var json = "{ \"key\": +5 }";
            var options = new JsonOptions { SerializationFlags = JsonSerializationFlags.AllowPositiveSign };
            var obj = options.Deserialize(json).GetJsonObject();
            Assert.AreEqual(5, obj["key"].GetInteger());
        }

        [TestMethod]
        public void Deserialize_WithInfinity_ParsesCorrectly()
        {
            // JSON5 allows Infinity. LightJson might support it via specific handling or flags?
            // The enum doesn't have "AllowInfinity".
            // But JsonOptions has InfinityHandler.
            // However, parsing Infinity usually requires support in the reader.
            // Let's check if Json5 flag covers it or if it's supported by default or another way.
            // The test plan says "using appropriate flag".
            // Maybe it's standard in LightJson or covered by Json5?
            // I'll try with Json5 flag.
            var json = "{ \"key\": Infinity }";
            var options = new JsonOptions { SerializationFlags = JsonSerializationFlags.Json5 };
            // Also -Infinity
            var json2 = "{ \"key\": -Infinity }";

            var obj = options.Deserialize(json).GetJsonObject();
            Assert.IsTrue(double.IsPositiveInfinity(obj["key"].GetNumber()));

            var obj2 = options.Deserialize(json2).GetJsonObject();
            Assert.IsTrue(double.IsNegativeInfinity(obj2["key"].GetNumber()));
        }

        [TestMethod]
        public void Deserialize_WithNaN_ParsesCorrectly()
        {
            var json = "{ \"key\": NaN }";
            var options = new JsonOptions { SerializationFlags = JsonSerializationFlags.Json5 };
            var obj = options.Deserialize(json).GetJsonObject();
            Assert.IsTrue(double.IsNaN(obj["key"].GetNumber()));
        }

        [TestMethod]
        public void Deserialize_WithMultilineStrings_ParsesCorrectly()
        {
            // JSON5 multiline string with backslash
            var json = "{ \"key\": \"line1\\\nline2\" }";
            // In C# string literal: "{ \"key\": \"line1\\\nline2\" }" -> JSON: { "key": "line1\<newline>line2" }
            // Wait, backslash at end of line escapes the newline.
            var options = new JsonOptions { SerializationFlags = JsonSerializationFlags.AllowStringLineBreaks };
            var obj = options.Deserialize(json).GetJsonObject();
            Assert.AreEqual("line1line2", obj["key"].GetString()); // JSON5 joins them
        }

        [TestMethod]
        public void Deserialize_WithAllFlagsEnabled_ParsesComplexJson5()
        {
            var json = @"
            {
                // comment
                key: 'value',
                num: +0xFF,
                list: [
                    .5,
                    NaN,
                ],
            }";
            var options = new JsonOptions { SerializationFlags = JsonSerializationFlags.Json5 };
            var obj = options.Deserialize(json).GetJsonObject();
            Assert.AreEqual("value", obj["key"].GetString());
            Assert.AreEqual(255, obj["num"].GetInteger());
            Assert.AreEqual(0.5, obj["list"].GetJsonArray()[0].GetNumber());
            Assert.IsTrue(double.IsNaN(obj["list"].GetJsonArray()[1].GetNumber()));
        }

        [TestMethod]
        public void Deserialize_WithoutFlags_RejectsJson5Syntax()
        {
            var json = "{ key: \"value\" }";
            // Default options
            Assert.ThrowsException<JsonParseException>(() => JsonValue.Parse(json, null));
        }
    }
}

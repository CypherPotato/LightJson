using LightJson;
using LightJson.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LightJsonTests
{
    [TestClass]
    public class InfinityHandlerTests
    {
        [TestMethod]
        public void InfinityHandler_Default_AllowsInfinity()
        {
            // Default is WriteNull
            var json = JsonValue.Serialize(double.PositiveInfinity);
            Assert.IsTrue(json.IsNull);
        }


        [TestMethod]
        public void InfinityHandler_AsString_SerializesInfinityAsString()
        {
            var options = new JsonOptions { InfinityHandler = JsonInfinityHandleOption.ReplaceWithString };
            var val = new JsonValue(double.PositiveInfinity);
            var jsonString = val.ToString(options);
            Assert.AreEqual("\"Infinity\"", jsonString);
        }

        [TestMethod]
        public void InfinityHandler_AsString_SerializesNegativeInfinityAsString()
        {
            var options = new JsonOptions { InfinityHandler = JsonInfinityHandleOption.ReplaceWithString };
            var val = new JsonValue(double.NegativeInfinity);
            var jsonString = val.ToString(options);
            Assert.AreEqual("\"-Infinity\"", jsonString);
        }

        [TestMethod]
        public void InfinityHandler_AsString_SerializesNaNAsString()
        {
            var options = new JsonOptions { InfinityHandler = JsonInfinityHandleOption.ReplaceWithString };
            var val = new JsonValue(double.NaN);
            var jsonString = val.ToString(options);
            Assert.AreEqual("\"NaN\"", jsonString);
        }

        [TestMethod]
        public void InfinityHandler_AsString_DeserializesInfinityString()
        {
            var json = "\"Infinity\"";
            var options = new JsonOptions { AllowNumbersAsStrings = true };
            var val = options.Deserialize(json);
            Assert.IsTrue(double.IsPositiveInfinity(val.GetNumber()));
        }

        [TestMethod]
        public void InfinityHandler_AsString_DeserializesNaNString()
        {
            var json = "\"NaN\"";
            var options = new JsonOptions { AllowNumbersAsStrings = true };
            var val = options.Deserialize(json);
            Assert.IsTrue(double.IsNaN(val.GetNumber()));
        }

        [TestMethod]
        public void InfinityHandler_AsNull_SerializesInfinityAsNull()
        {
            var options = new JsonOptions { InfinityHandler = JsonInfinityHandleOption.WriteNull };
            var val = new JsonValue(double.PositiveInfinity);
            var jsonString = val.ToString(options);
            Assert.AreEqual("null", jsonString);
        }

        [TestMethod]
        public void InfinityHandler_AsNull_SerializesNaNAsNull()
        {
            var options = new JsonOptions { InfinityHandler = JsonInfinityHandleOption.WriteNull };
            var val = new JsonValue(double.NaN);
            var jsonString = val.ToString(options);
            Assert.AreEqual("null", jsonString);
        }

        [TestMethod]
        public void InfinityHandler_ThrowException_ThrowsOnInfinity()
        {
            var options = new JsonOptions { InfinityHandler = JsonInfinityHandleOption.ThrowException };
            var val = new JsonValue(double.PositiveInfinity);
            Assert.ThrowsException<JsonSerializationException>(() => val.ToString(options));
        }
    }
}

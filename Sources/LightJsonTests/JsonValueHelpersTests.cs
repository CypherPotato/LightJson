using LightJson;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace LightJsonTests
{
    [TestClass]
    public class JsonValueHelpersTests
    {
        [TestMethod]
        public void TryGet_WithValidType_ReturnsTypedValue()
        {
            var json = new JsonValue(42);
            var result = json.TryGet<int>();
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        public void TryGet_WithInvalidType_ReturnsNull()
        {
            var json = new JsonValue("not a number");
            var result = json.TryGet<int>();
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TryGet_WithNullValue_ReturnsNull()
        {
            var json = JsonValue.Null;
            var result = json.TryGet<string>();
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TryGet_WithUndefinedValue_ReturnsNull()
        {
            var json = JsonValue.Undefined;
            var result = json.TryGet<string>();
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TryGet_WithArrayIndex_ReturnsElementValue()
        {
            JsonValue json = new JsonArray { "first", "second" };
            var result = json.TryGet<string>(1);
            Assert.AreEqual("second", result);
        }

        [TestMethod]
        public void TryGet_WithPropertyName_ReturnsPropertyValue()
        {
            JsonValue json = new JsonObject { ["age"] = 30 };
            var result = json.TryGet<int>("age");
            Assert.AreEqual(30, result);
        }

        // Skipping TryGet_WithComplexType_DeserializesObject for now as it requires a custom class and setup

        [TestMethod]
        public void TryEvaluate_WithValidFunction_ReturnsEvaluatedResult()
        {
            var json = new JsonValue(123);
            var result = json.TryEvaluate(val => val.ToString());
            Assert.AreEqual("123", result);
        }

        [TestMethod]
        public void TryEvaluate_WithExceptionThrowingFunction_ReturnsDefaultValue()
        {
            var json = new JsonValue(123);
            var result = json.TryEvaluate<string>(val => throw new Exception(), "default");
            Assert.AreEqual("default", result);
        }

        [TestMethod]
        public void TryEvaluate_WithUndefinedValue_ReturnsDefaultValue()
        {
            var json = JsonValue.Undefined;
            var result = json.TryEvaluate(val => val.ToString(), "default");
            Assert.AreEqual("default", result);
        }

        [TestMethod]
        public void TryEvaluateFirst_WithMultipleFunctions_ReturnsFirstSuccess()
        {
            var json = new JsonValue(123);
            var result = json.TryEvaluateFirst(new Func<JsonValue, string?>[] {
                val => throw new Exception(),
                val => val.ToString()
            });
            Assert.AreEqual("123", result);
        }

        [TestMethod]
        public void TryEvaluateFirst_WithAllFailingFunctions_ReturnsDefaultValue()
        {
            var json = new JsonValue(123);
            var result = json.TryEvaluateFirst<string>(
                new Func<JsonValue, string?>[] {
                    val => throw new Exception(),
                    val => throw new Exception()
                },
                "default"
            );
            Assert.AreEqual("default", result);
        }

        [TestMethod]
        public void Get_WithValidType_ReturnsTypedValue()
        {
            var json = new JsonValue(true);
            var result = json.Get<bool>();
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Get_WithNullableType_HandlesNullValue()
        {
            var json = JsonValue.Null;
            var result = json.Get<int?>();
            Assert.IsNull(result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidCastException))]
        public void Get_WithIncompatibleType_ThrowsException()
        {
            var json = new JsonValue("not a number");
            json.Get<int>();
        }
    }
}

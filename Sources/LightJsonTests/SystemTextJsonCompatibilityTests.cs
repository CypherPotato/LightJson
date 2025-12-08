using LightJson;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;

namespace LightJsonTests
{
    [TestClass]
    public class SystemTextJsonCompatibilityTests
    {
        [TestMethod]
        public void SystemTextJson_SerializeJsonValue_ProducesValidJson()
        {
            var val = new JsonValue("Test");
            var json = System.Text.Json.JsonSerializer.Serialize(val);
            Assert.AreEqual("\"Test\"", json);
        }

        [TestMethod]
        public void SystemTextJson_DeserializeToJsonValue_Works()
        {
            var json = "\"Test\"";
            var val = System.Text.Json.JsonSerializer.Deserialize<JsonValue>(json);
            Assert.AreEqual("Test", val.GetString());
        }

        [TestMethod]
        public void SystemTextJson_RoundTripJsonValue_PreservesData()
        {
            var val = new JsonValue(123);
            var json = System.Text.Json.JsonSerializer.Serialize(val);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<JsonValue>(json);
            Assert.AreEqual(123, deserialized.GetInteger());
        }

        [TestMethod]
        public void SystemTextJson_SerializeJsonObject_ProducesValidJson()
        {
            var obj = new JsonObject { ["Key"] = "Value" };
            var json = System.Text.Json.JsonSerializer.Serialize(obj);
            Assert.IsTrue(json.Contains("\"Key\":\"Value\"") || json.Contains("\"Key\": \"Value\""));
        }

        [TestMethod]
        public void SystemTextJson_DeserializeToJsonObject_Works()
        {
            var json = "{\"Key\": \"Value\"}";
            var obj = System.Text.Json.JsonSerializer.Deserialize<JsonObject>(json);
            Assert.IsNotNull(obj);
            Assert.AreEqual("Value", obj["Key"].GetString());
        }

        [TestMethod]
        public void SystemTextJson_RoundTripJsonObject_PreservesData()
        {
            var obj = new JsonObject { ["Key"] = 123 };
            var json = System.Text.Json.JsonSerializer.Serialize(obj);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<JsonObject>(json);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(123, deserialized["Key"].GetInteger());
        }

        [TestMethod]
        public void SystemTextJson_SerializeJsonArray_ProducesValidJson()
        {
            var arr = new JsonArray { 1, 2, 3 };
            var json = System.Text.Json.JsonSerializer.Serialize(arr);
            Assert.AreEqual("[1,2,3]", json);
        }

        [TestMethod]
        public void SystemTextJson_DeserializeToJsonArray_Works()
        {
            var json = "[1, 2, 3]";
            var arr = System.Text.Json.JsonSerializer.Deserialize<JsonArray>(json);
            Assert.IsNotNull(arr);
            Assert.AreEqual(3, arr.Count);
            Assert.AreEqual(1, arr[0].GetInteger());
        }

        [TestMethod]
        public void SystemTextJson_RoundTripJsonArray_PreservesData()
        {
            var arr = new JsonArray { "A", "B" };
            var json = System.Text.Json.JsonSerializer.Serialize(arr);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<JsonArray>(json);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("A", deserialized[0].GetString());
            Assert.AreEqual("B", deserialized[1].GetString());
        }
    }
}

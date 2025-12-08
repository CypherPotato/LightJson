using LightJson;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LightJsonTests
{
    [TestClass]
    public class IJsonSerializableTests
    {
        private class MockSerializable : IJsonSerializable<MockSerializable>
        {
            public string? Value { get; set; }

            public static JsonValue SerializeIntoJson(MockSerializable self, JsonOptions options)
            {
                return new JsonObject { ["value"] = self.Value };
            }

            public static MockSerializable DeserializeFromJson(JsonValue json, JsonOptions options)
            {
                var val = json["value"];
                return new MockSerializable { Value = val.IsNull ? null : val.GetString() };
            }
        }

        private class OptionsSerializable : IJsonSerializable<OptionsSerializable>
        {
            public string? Value { get; set; }

            public static JsonValue SerializeIntoJson(OptionsSerializable self, JsonOptions options)
            {
                if (options.WriteIndented)
                {
                    return new JsonObject { ["indented"] = true, ["value"] = self.Value };
                }
                return new JsonObject { ["indented"] = false, ["value"] = self.Value };
            }

            public static OptionsSerializable DeserializeFromJson(JsonValue json, JsonOptions options)
            {
                return new OptionsSerializable { Value = json["value"].GetString() };
            }
        }

        private class Wrapper
        {
            public MockSerializable? Item { get; set; }
        }

        [TestMethod]
        public void IJsonSerializable_Serialize_CallsStaticMethod()
        {
            var obj = new MockSerializable { Value = "test" };
            var json = JsonValue.Serialize(obj);
            Assert.IsTrue(json.IsJsonObject);
            Assert.AreEqual("test", json["value"].GetString());
        }

        [TestMethod]
        public void IJsonSerializable_Deserialize_CallsStaticMethod()
        {
            var json = new JsonObject { ["value"] = "test" };
            var jsonValue = new JsonValue(json);
            var obj = jsonValue.Get<MockSerializable>();
            Assert.AreEqual("test", obj.Value);
        }

        [TestMethod]
        public void IJsonSerializable_RoundTrip_PreservesData()
        {
            var original = new MockSerializable { Value = "roundtrip" };
            var json = JsonValue.Serialize(original);
            var deserialized = json.Get<MockSerializable>();
            Assert.AreEqual("roundtrip", deserialized.Value);
        }

        [TestMethod]
        public void IJsonSerializable_ReceivesOptions_UsesOptions()
        {
            var obj = new OptionsSerializable { Value = "test" };

            var options1 = new JsonOptions { WriteIndented = true };
            var json1 = JsonValue.Serialize(obj, options1);
            Assert.IsTrue(json1["indented"].GetBoolean());

            var options2 = new JsonOptions { WriteIndented = false };
            var json2 = JsonValue.Serialize(obj, options2);
            Assert.IsFalse(json2["indented"].GetBoolean());
        }

        [TestMethod]
        public void IJsonSerializable_WithNullableProperties_HandlesNull()
        {
            var obj = new MockSerializable { Value = null };
            var json = JsonValue.Serialize(obj);
            Assert.IsTrue(json["value"].IsNull);

            var deserialized = json.Get<MockSerializable>();
            Assert.IsNull(deserialized.Value);
        }

        [TestMethod]
        public void IJsonSerializable_AsArrayElement_SerializesCorrectly()
        {
            var arr = new MockSerializable[]
            {
                new MockSerializable { Value = "1" },
                new MockSerializable { Value = "2" }
            };
            var json = JsonValue.Serialize(arr);
            Assert.IsTrue(json.IsJsonArray);
            Assert.AreEqual(2, json.GetJsonArray().Count);
            Assert.AreEqual("1", json[0]["value"].GetString());
        }

        [TestMethod]
        public void IJsonSerializable_AsPropertyValue_SerializesCorrectly_WithWrapper()
        {
            var wrapper = new Wrapper { Item = new MockSerializable { Value = "test" } };
            var json = JsonValue.Serialize(wrapper);
            Assert.AreEqual("test", json["Item"]["value"].GetString());
        }
    }
}

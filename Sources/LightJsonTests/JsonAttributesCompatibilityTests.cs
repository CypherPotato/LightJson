using LightJson;
using LightJson.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json.Serialization;

namespace LightJsonTests
{
    [TestClass]
    public class JsonAttributesCompatibilityTests
    {
        private class NamedPropertyHolder
        {
            [JsonPropertyName("custom_name")]
            public string? Name { get; set; }
        }

        [TestMethod]
        public void JsonPropertyName_Serialize_UsesCustomName()
        {
            var holder = new NamedPropertyHolder { Name = "Value" };
            var json = JsonValue.Serialize(holder).ToString();
            Assert.IsTrue(json.Contains("\"custom_name\""));
        }

        [TestMethod]
        public void JsonPropertyName_Deserialize_MapsCustomName()
        {
            var json = "{\"custom_name\": \"Value\"}";
            var holder = JsonValue.Parse(json, null).Get<NamedPropertyHolder>();
            Assert.AreEqual("Value", holder.Name);
        }

        private class IgnoredPropertyHolder
        {
            public string? Visible { get; set; }
            [JsonIgnore]
            public string? Hidden { get; set; }
        }

        [TestMethod]
        public void JsonIgnore_Serialize_OmitsProperty()
        {
            var holder = new IgnoredPropertyHolder { Visible = "Yes", Hidden = "No" };
            var json = JsonValue.Serialize(holder).ToString();
            Assert.IsTrue(json.Contains("\"Visible\""));
            Assert.IsFalse(json.Contains("\"Hidden\""));
        }

        [TestMethod]
        public void JsonIgnore_Deserialize_SkipsProperty()
        {
            var json = "{\"Visible\": \"Yes\", \"Hidden\": \"No\"}";
            var holder = JsonValue.Parse(json, null).Get<IgnoredPropertyHolder>();
            Assert.AreEqual("Yes", holder.Visible);
            Assert.IsNull(holder.Hidden);
        }

        private class IgnoreNullHolder
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? NullableProp { get; set; }
        }

        [TestMethod]
        public void JsonIgnoreCondition_WhenNull_IgnoresNullValues()
        {
            var holder = new IgnoreNullHolder { NullableProp = null };
            var json = JsonValue.Serialize(holder).ToString();
            Assert.IsFalse(json.Contains("\"NullableProp\""));

            holder.NullableProp = "Value";
            json = JsonValue.Serialize(holder).ToString();
            Assert.IsTrue(json.Contains("\"NullableProp\""));
        }

        private class IncludePrivateHolder
        {
            [JsonInclude]
            public string? PrivateProp { get; private set; }

            public IncludePrivateHolder(string val)
            {
                PrivateProp = val;
            }
        }

        [TestMethod]
        public void JsonInclude_IncludesPrivateProperty()
        {
            var holder = new IncludePrivateHolder("Secret");
            var json = JsonValue.Serialize(holder).ToString();
            Assert.IsTrue(json.Contains("\"PrivateProp\""));
            Assert.IsTrue(json.Contains("\"Secret\""));
        }
    }
}

using LightJson;
using LightJson.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace LightJsonTests
{
    [TestClass]
    public class AotSerializationTests
    {
        public class AotPerson
        {
            public string? Name { get; set; }
            public int Age { get; set; }
        }

        [TestMethod]
        public void AotSerialization_WithSourceGeneratedContext_Serializes()
        {
            var person = new AotPerson { Name = "AOT", Age = 42 };
            var resolver = new DefaultJsonTypeInfoResolver();
            var options = new JsonOptions
            {
                SerializerContext = new JsonOptionsSerializerContext(resolver, new System.Text.Json.JsonSerializerOptions())
            };

            var jsonVal = JsonValue.Serialize(person, options);
            Assert.AreEqual("AOT", jsonVal["Name"].GetString());
            Assert.AreEqual(42, jsonVal["Age"].GetInteger());
        }

        [TestMethod]
        public void AotDeserialization_WithSourceGeneratedContext_Deserializes()
        {
            var json = "{\"Name\": \"AOT\", \"Age\": 42}";
            var resolver = new DefaultJsonTypeInfoResolver();
            var options = new JsonOptions
            {
                SerializerContext = new JsonOptionsSerializerContext(resolver, new System.Text.Json.JsonSerializerOptions())
            };

            var person = options.Deserialize(json).Get<AotPerson>();
            Assert.AreEqual("AOT", person.Name);
            Assert.AreEqual(42, person.Age);
        }
    }
}

using LightJson;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LightJsonTests
{
    [TestClass]
    public partial class AotSerializationTests
    {
        public class AotPerson
        {
            public string? Name { get; set; }
            public int Age { get; set; }
        }

        public class NotMappedAotPerson
        {
            public string? Name { get; set; }
        }

        [JsonSerializable(typeof(AotPerson))]
        internal partial class MyAotContext : JsonSerializerContext
        {
        }

        [TestMethod]
        public void AotSerialization_WithGeneratedContext_Serializes()
        {
            var person = new AotPerson { Name = "AOT Generated", Age = 99 };
            var options = new JsonOptions
            {
                SerializerContext = MyAotContext.Default
            };

            var jsonVal = JsonValue.Serialize(person, options);
            Assert.AreEqual("AOT Generated", jsonVal["Name"].GetString());
            Assert.AreEqual(99, jsonVal["Age"].GetInteger());
        }

        [TestMethod]
        public void AotDeserialization_WithGeneratedContext_Deserializes()
        {
            var json = "{\"Name\": \"AOT Generated\", \"Age\": 99}";
            var options = new JsonOptions
            {
                SerializerContext = MyAotContext.Default
            };

            var person = options.Deserialize(json).Get<AotPerson>();
            Assert.AreEqual("AOT Generated", person.Name);
            Assert.AreEqual(99, person.Age);
        }

        [TestMethod]
        public void AotDeserialization_WithoutTypeInfo_Throws()
        {
            var json = "{\"Name\": \"Unknown\"}";
            var options = new JsonOptions
            {
                SerializerContext = MyAotContext.Default
            };

            Assert.ThrowsException<JsonException>(() =>
            {
                options.Deserialize(json).Get<NotMappedAotPerson>();
            });
        }
    }
}

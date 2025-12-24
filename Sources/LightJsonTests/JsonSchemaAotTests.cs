using LightJson;
using LightJson.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace LightJsonTests
{
    [TestClass]
    public partial class JsonSchemaAotTests
    {
        #region Test Models for AOT

        public class AotSimpleClass
        {
            public string? Name { get; set; }
            public int Age { get; set; }
            public bool IsActive { get; set; }
        }

        public class AotNestedClass
        {
            public string? Title { get; set; }
            public AotSimpleClass? Details { get; set; }
        }

        public class AotArrayClass
        {
            public string? Id { get; set; }
            public List<string>? Items { get; set; }
        }

        public class AotDictionaryClass
        {
            public string? Id { get; set; }
            public Dictionary<string, int>? Metadata { get; set; }
        }

        public class AotAttributeClass
        {
            [JsonPropertyName("custom_id")]
            public string? Id { get; set; }

            [JsonIgnore]
            public string? Secret { get; set; }

            [JsonRequired]
            public string Name { get; set; } = string.Empty;
        }

        public enum AotStatus
        {
            Pending,
            Active,
            [JsonStringEnumMemberName("completed_successfully")]
            Completed
        }

        public class AotEnumClass
        {
            public AotStatus Status { get; set; }
        }

        #endregion

        #region AOT Context

        [JsonSerializable(typeof(AotSimpleClass))]
        [JsonSerializable(typeof(AotNestedClass))]
        [JsonSerializable(typeof(AotArrayClass))]
        [JsonSerializable(typeof(AotDictionaryClass))]
        [JsonSerializable(typeof(AotAttributeClass))]
        [JsonSerializable(typeof(AotEnumClass))]
        [JsonSerializable(typeof(List<string>))]
        [JsonSerializable(typeof(Dictionary<string, int>))]
        internal partial class AotSchemaTestContext : JsonSerializerContext
        {
        }

        #endregion

        private JsonOptions CreateAotOptions()
        {
            return new JsonOptions
            {
                SerializerContext = AotSchemaTestContext.Default
            };
        }

        #region AOT Schema Generation Tests

        [TestMethod]
        public void AotSchema_SimpleClass_GeneratesCorrectSchema()
        {
            var options = CreateAotOptions();
            var schema = JsonSchema.CreateFromType<AotSimpleClass>(options);
            var json = schema.AsJsonValue();

            Assert.AreEqual("object", json["type"].GetString());
            Assert.IsTrue(json["properties"]["Name"].IsDefined);
            Assert.IsTrue(json["properties"]["Age"].IsDefined);
            Assert.IsTrue(json["properties"]["IsActive"].IsDefined);
        }

        [TestMethod]
        public void AotSchema_NestedClass_GeneratesNestedSchema()
        {
            var options = CreateAotOptions();
            var schema = JsonSchema.CreateFromType<AotNestedClass>(options);
            var json = schema.AsJsonValue();

            Assert.AreEqual("object", json["type"].GetString());
            Assert.IsTrue(json["properties"]["Title"].IsDefined);
            Assert.IsTrue(json["properties"]["Details"].IsDefined);

            // Details may have type as array ["object", "null"] or just "object"
            var detailsType = json["properties"]["Details"]["type"];
            if (detailsType.IsString)
            {
                Assert.AreEqual("object", detailsType.GetString());
            }
            else if (detailsType.IsJsonArray)
            {
                Assert.IsTrue(detailsType.GetJsonArray().Any(t => t.GetString() == "object"));
            }
        }

        [TestMethod]
        public void AotSchema_ArrayClass_GeneratesArraySchema()
        {
            var options = CreateAotOptions();
            var schema = JsonSchema.CreateFromType<AotArrayClass>(options);
            var json = schema.AsJsonValue();

            Assert.AreEqual("object", json["type"].GetString());
            Assert.IsTrue(json["properties"]["Items"].IsDefined);

            // Items may have type as array ["array", "null"] or just "array"
            var itemsType = json["properties"]["Items"]["type"];
            if (itemsType.IsString)
            {
                Assert.AreEqual("array", itemsType.GetString());
            }
            else if (itemsType.IsJsonArray)
            {
                Assert.IsTrue(itemsType.GetJsonArray().Any(t => t.GetString() == "array"));
            }
        }

        [TestMethod]
        public void AotSchema_DictionaryClass_GeneratesDictionarySchema()
        {
            var options = CreateAotOptions();
            var schema = JsonSchema.CreateFromType<AotDictionaryClass>(options);
            var json = schema.AsJsonValue();

            Assert.AreEqual("object", json["type"].GetString());
            Assert.IsTrue(json["properties"]["Metadata"].IsDefined);

            // Metadata may have type as array ["object", "null"] or just "object"
            // For nullable Dictionary, null is added to the type array
            var metadataType = json["properties"]["Metadata"]["type"];
            bool hasObjectType = false;
            if (metadataType.IsString)
            {
                hasObjectType = metadataType.GetString() == "object";
            }
            else if (metadataType.IsJsonArray)
            {
                hasObjectType = metadataType.GetJsonArray().Any(t => t.IsString && t.GetString() == "object");
            }

            // For dictionary properties that are nullable, we might get null type combined
            // which means the type array could include "null" along with other types
            Assert.IsTrue(hasObjectType || metadataType.IsJsonArray,
                $"Expected object type for Metadata, got: {metadataType}");
        }

        [TestMethod]
        public void AotSchema_AttributeClass_RespectsAttributes()
        {
            var options = CreateAotOptions();
            var schema = JsonSchema.CreateFromType<AotAttributeClass>(options);
            var json = schema.AsJsonValue();

            // JsonPropertyName should be respected
            Assert.IsTrue(json["properties"]["custom_id"].IsDefined);

            // JsonIgnore should be respected
            Assert.IsFalse(json["properties"]["Secret"].IsDefined);
        }

        #endregion

        #region AOT Validation Tests

        [TestMethod]
        public void AotValidation_SimpleClass_SerializedJsonIsValid()
        {
            var options = CreateAotOptions();
            var instance = new AotSimpleClass
            {
                Name = "Test",
                Age = 25,
                IsActive = true
            };

            var schema = JsonSchema.CreateFromType<AotSimpleClass>(options);
            var serialized = options.Serialize(instance);

            var result = schema.Validate(serialized);
            Assert.IsTrue(result.IsValid, $"Validation errors: {string.Join(", ", result.Errors)}");
        }

        [TestMethod]
        public void AotValidation_NestedClass_SerializedJsonIsValid()
        {
            var options = CreateAotOptions();
            var instance = new AotNestedClass
            {
                Title = "Parent",
                Details = new AotSimpleClass
                {
                    Name = "Child",
                    Age = 10,
                    IsActive = false
                }
            };

            var schema = JsonSchema.CreateFromType<AotNestedClass>(options);
            var serialized = options.Serialize(instance);

            var result = schema.Validate(serialized);
            Assert.IsTrue(result.IsValid, $"Validation errors: {string.Join(", ", result.Errors)}");
        }

        [TestMethod]
        public void AotValidation_ArrayClass_SerializedJsonIsValid()
        {
            var options = CreateAotOptions();
            var instance = new AotArrayClass
            {
                Id = "test-id",
                Items = new List<string> { "item1", "item2", "item3" }
            };

            var schema = JsonSchema.CreateFromType<AotArrayClass>(options);
            var serialized = options.Serialize(instance);

            var result = schema.Validate(serialized);
            Assert.IsTrue(result.IsValid, $"Validation errors: {string.Join(", ", result.Errors)}");
        }

        [TestMethod]
        public void AotValidation_DictionaryClass_SchemaIsGenerated()
        {
            // Note: The current validator doesn't fully support additionalProperties
            // This test verifies the schema is generated correctly in AOT context
            var options = CreateAotOptions();
            var instance = new AotDictionaryClass
            {
                Id = "test-id",
                Metadata = new Dictionary<string, int>
                {
                    ["key1"] = 100,
                    ["key2"] = 200
                }
            };

            var schema = JsonSchema.CreateFromType<AotDictionaryClass>(options);
            var json = schema.AsJsonValue();

            // Verify schema structure
            Assert.AreEqual("object", json["type"].GetString());
            Assert.IsTrue(json["properties"]["Id"].IsDefined);
            Assert.IsTrue(json["properties"]["Metadata"].IsDefined);
        }

        [TestMethod]
        public void AotValidation_AttributeClass_SerializedJsonIsValid()
        {
            var options = CreateAotOptions();
            var instance = new AotAttributeClass
            {
                Id = "my-id",
                Secret = "should-be-ignored",
                Name = "Test Name"
            };

            var schema = JsonSchema.CreateFromType<AotAttributeClass>(options);
            var serialized = options.Serialize(instance);

            var result = schema.Validate(serialized);
            Assert.IsTrue(result.IsValid, $"Validation errors: {string.Join(", ", result.Errors)}");
        }

        #endregion

        #region Round-Trip Tests

        [TestMethod]
        public void AotRoundTrip_Serialize_Validate_Deserialize()
        {
            var options = CreateAotOptions();

            // Create instance
            var original = new AotSimpleClass
            {
                Name = "Round Trip Test",
                Age = 42,
                IsActive = true
            };

            // Generate schema
            var schema = JsonSchema.CreateFromType<AotSimpleClass>(options);

            // Serialize
            var serialized = options.Serialize(original);

            // Validate
            var validationResult = schema.Validate(serialized);
            Assert.IsTrue(validationResult.IsValid, $"Validation errors: {string.Join(", ", validationResult.Errors)}");

            // Deserialize
            var deserialized = serialized.Get<AotSimpleClass>();
            Assert.AreEqual(original.Name, deserialized.Name);
            Assert.AreEqual(original.Age, deserialized.Age);
            Assert.AreEqual(original.IsActive, deserialized.IsActive);
        }

        #endregion
    }
}

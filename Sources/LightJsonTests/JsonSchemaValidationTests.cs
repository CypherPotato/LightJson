using LightJson;
using LightJson.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace LightJsonTests
{
    [TestClass]
    public class JsonSchemaValidationTests
    {
        [TestMethod]
        public void JsonSchema_ValidateString_SucceedsForValidString()
        {
            var schema = JsonSchema.CreateStringSchema();
            var result = schema.Validate(new JsonValue("test"));
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void JsonSchema_ValidateString_FailsForNonString()
        {
            var schema = JsonSchema.CreateStringSchema();
            var result = schema.Validate(new JsonValue(123));
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors[0].Message.Contains("is not one of the expected types"));
        }

        [TestMethod]
        public void JsonSchema_StringMinLength_ValidatesMinimum()
        {
            var schema = JsonSchema.CreateStringSchema(minLength: 3);
            Assert.IsTrue(schema.Validate(new JsonValue("abc")).IsValid);
            Assert.IsFalse(schema.Validate(new JsonValue("ab")).IsValid);
        }

        [TestMethod]
        public void JsonSchema_StringMaxLength_ValidatesMaximum()
        {
            var schema = JsonSchema.CreateStringSchema(maxLength: 3);
            Assert.IsTrue(schema.Validate(new JsonValue("abc")).IsValid);
            Assert.IsFalse(schema.Validate(new JsonValue("abcd")).IsValid);
        }

        [TestMethod]
        public void JsonSchema_StringPattern_ValidatesRegex()
        {
            var schema = JsonSchema.CreateStringSchema(pattern: "^[a-z]+$");
            Assert.IsTrue(schema.Validate(new JsonValue("abc")).IsValid);
            Assert.IsFalse(schema.Validate(new JsonValue("123")).IsValid);
        }

        [TestMethod]
        public void JsonSchema_StringFormat_ValidatesFormat()
        {
            var schema = JsonSchema.CreateStringSchema(format: "email");
            Assert.IsTrue(schema.Validate(new JsonValue("test@example.com")).IsValid);
            Assert.IsFalse(schema.Validate(new JsonValue("invalid-email")).IsValid);
        }

        [TestMethod]
        public void JsonSchema_StringEnum_ValidatesEnumValues()
        {
            var schema = JsonSchema.CreateStringSchema(enums: new[] { "A", "B" });
            Assert.IsTrue(schema.Validate(new JsonValue("A")).IsValid);
            Assert.IsFalse(schema.Validate(new JsonValue("C")).IsValid);
        }

        [TestMethod]
        public void JsonSchema_Number_ValidatesNumericType()
        {
            var schema = JsonSchema.CreateNumberSchema();
            Assert.IsTrue(schema.Validate(new JsonValue(123)).IsValid);
            Assert.IsFalse(schema.Validate(new JsonValue("123")).IsValid);
        }

        [TestMethod]
        public void JsonSchema_NumberMinimum_ValidatesMinimum()
        {
            var schema = JsonSchema.CreateNumberSchema(minimum: 10);
            Assert.IsTrue(schema.Validate(new JsonValue(10)).IsValid);
            Assert.IsFalse(schema.Validate(new JsonValue(9)).IsValid);
        }

        [TestMethod]
        public void JsonSchema_NumberMaximum_ValidatesMaximum()
        {
            var schema = JsonSchema.CreateNumberSchema(maximum: 10);
            Assert.IsTrue(schema.Validate(new JsonValue(10)).IsValid);
            Assert.IsFalse(schema.Validate(new JsonValue(11)).IsValid);
        }

        [TestMethod]
        public void JsonSchema_NumberExclusiveMinimum_ValidatesExclusiveMinimum()
        {
            var schema = JsonSchema.CreateNumberSchema(exclusiveMinimum: 10);
            Assert.IsTrue(schema.Validate(new JsonValue(11)).IsValid);
            Assert.IsFalse(schema.Validate(new JsonValue(10)).IsValid);
        }

        [TestMethod]
        public void JsonSchema_NumberExclusiveMaximum_ValidatesExclusiveMaximum()
        {
            var schema = JsonSchema.CreateNumberSchema(exclusiveMaximum: 10);
            Assert.IsTrue(schema.Validate(new JsonValue(9)).IsValid);
            Assert.IsFalse(schema.Validate(new JsonValue(10)).IsValid);
        }

        [TestMethod]
        public void JsonSchema_NumberMultipleOf_ValidatesMultipleOf()
        {
            var schema = JsonSchema.CreateNumberSchema(multipleOf: 2);
            Assert.IsTrue(schema.Validate(new JsonValue(4)).IsValid);
            Assert.IsFalse(schema.Validate(new JsonValue(3)).IsValid);
        }
    }
}

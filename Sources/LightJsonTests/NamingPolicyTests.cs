using LightJson;
using LightJson.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace LightJsonTests
{
    [TestClass]
    public class NamingPolicyTests
    {
        public class Person
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public int Age { get; set; }
        }

        public class DictionaryHolder
        {
            public Dictionary<string, string>? Data { get; set; }
        }

        [TestMethod]
        public void NamingPolicy_CamelCase_ConvertsToCamelCase()
        {
            var person = new Person { FirstName = "John", LastName = "Doe", Age = 30 };
            var options = new JsonOptions { NamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonValue.Serialize(person, options).ToString();

            Assert.IsTrue(json.Contains("\"firstName\""));
            Assert.IsTrue(json.Contains("\"lastName\""));
            Assert.IsTrue(json.Contains("\"age\""));
        }

        [TestMethod]
        public void NamingPolicy_CamelCase_Deserialize_ConvertsFromCamelCase()
        {
            var json = "{\"firstName\":\"John\",\"lastName\":\"Doe\",\"age\":30}";
            var options = new JsonOptions { NamingPolicy = JsonNamingPolicy.CamelCase };
            var person = options.Deserialize(json).Get<Person>();

            Assert.AreEqual("John", person.FirstName);
            Assert.AreEqual("Doe", person.LastName);
            Assert.AreEqual(30, person.Age);
        }

        [TestMethod]
        public void NamingPolicy_SnakeLower_ConvertsToSnakeCase()
        {
            var person = new Person { FirstName = "John", LastName = "Doe", Age = 30 };
            var options = new JsonOptions { NamingPolicy = JsonNamingPolicy.SnakeCaseLower };
            var json = JsonValue.Serialize(person, options).ToString();

            Assert.IsTrue(json.Contains("\"first_name\""));
            Assert.IsTrue(json.Contains("\"last_name\""));
            Assert.IsTrue(json.Contains("\"age\""));
        }

        [TestMethod]
        public void NamingPolicy_SnakeUpper_ConvertsToUpperSnakeCase()
        {
            var person = new Person { FirstName = "John", LastName = "Doe", Age = 30 };
            var options = new JsonOptions { NamingPolicy = JsonNamingPolicy.SnakeCaseUpper };
            var json = JsonValue.Serialize(person, options).ToString();

            Assert.IsTrue(json.Contains("\"FIRST_NAME\""));
            Assert.IsTrue(json.Contains("\"LAST_NAME\""));
            Assert.IsTrue(json.Contains("\"AGE\""));
        }

        [TestMethod]
        public void NamingPolicy_KebabLower_ConvertsToKebabCase()
        {
            var person = new Person { FirstName = "John", LastName = "Doe", Age = 30 };
            var options = new JsonOptions { NamingPolicy = JsonNamingPolicy.KebabCaseLower };
            var json = JsonValue.Serialize(person, options).ToString();

            Assert.IsTrue(json.Contains("\"first-name\""));
            Assert.IsTrue(json.Contains("\"last-name\""));
            Assert.IsTrue(json.Contains("\"age\""));
        }

        [TestMethod]
        public void NamingPolicy_KebabUpper_ConvertsToUpperKebabCase()
        {
            var person = new Person { FirstName = "John", LastName = "Doe", Age = 30 };
            var options = new JsonOptions { NamingPolicy = JsonNamingPolicy.KebabCaseUpper };
            var json = JsonValue.Serialize(person, options).ToString();

            Assert.IsTrue(json.Contains("\"FIRST-NAME\""));
            Assert.IsTrue(json.Contains("\"LAST-NAME\""));
            Assert.IsTrue(json.Contains("\"AGE\""));
        }

        [TestMethod]
        public void NamingPolicy_Null_PreservesOriginalNames()
        {
            var person = new Person { FirstName = "John", LastName = "Doe", Age = 30 };
            var options = new JsonOptions { NamingPolicy = null };
            var json = JsonValue.Serialize(person, options).ToString();

            Assert.IsTrue(json.Contains("\"FirstName\""));
            Assert.IsTrue(json.Contains("\"LastName\""));
            Assert.IsTrue(json.Contains("\"Age\""));
        }

        [TestMethod]
        public void NamingPolicy_CustomPolicy_UsesCustomLogic()
        {
            var person = new Person { FirstName = "John", LastName = "Doe", Age = 30 };
            var options = new JsonOptions { NamingPolicy = new CustomNamingPolicy() };
            var json = JsonValue.Serialize(person, options).ToString();

            Assert.IsTrue(json.Contains("\"PREFIX_FirstName\""));
            Assert.IsTrue(json.Contains("\"PREFIX_LastName\""));
            Assert.IsTrue(json.Contains("\"PREFIX_Age\""));
        }

        private class CustomNamingPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name)
            {
                return "PREFIX_" + name;
            }
        }

        [TestMethod]
        public void NamingPolicy_RoundTrip_WithSamePolicy_Works()
        {
            var person = new Person { FirstName = "John", LastName = "Doe", Age = 30 };
            var options = new JsonOptions { NamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonValue.Serialize(person, options).ToString();
            var deserialized = options.Deserialize(json).Get<Person>();

            Assert.AreEqual(person.FirstName, deserialized.FirstName);
            Assert.AreEqual(person.LastName, deserialized.LastName);
            Assert.AreEqual(person.Age, deserialized.Age);
        }

        [TestMethod]
        public void NamingPolicy_WithJsonObject_AppliesPolicy()
        {
            var obj = new JsonObject { ["MyKey"] = "value" };
            var options = new JsonOptions { NamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonValue.Serialize(obj, options).ToString();

            Assert.IsTrue(json.Contains("\"myKey\""));
        }

        [TestMethod]
        public void PreserveDictionaryNamingPolicy_True_SkipsPolicyForDictionaries()
        {
            var dict = new Dictionary<string, string> { ["MyKey"] = "value" };
            var options = new JsonOptions
            {
                NamingPolicy = JsonNamingPolicy.CamelCase,
                PreserveDictionaryNamingPolicy = true
            };
            var json = JsonValue.Serialize(dict, options).ToString();

            Assert.IsTrue(json.Contains("\"MyKey\""));
        }

        [TestMethod]
        public void PreserveDictionaryNamingPolicy_False_AppliesPolicyToDictionaries()
        {
            var dict = new Dictionary<string, string> { ["MyKey"] = "value" };
            var options = new JsonOptions
            {
                NamingPolicy = JsonNamingPolicy.CamelCase,
                PreserveDictionaryNamingPolicy = false
            };
            var json = JsonValue.Serialize(dict, options).ToString();

            Assert.IsTrue(json.Contains("\"myKey\""));
        }

        [TestMethod]
        public void NamingPolicy_WithNestedObjects_AppliesRecursively()
        {
            var nested = new { NestedProp = "value" };
            var root = new { RootProp = nested };
            var options = new JsonOptions { NamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonValue.Serialize(root, options).ToString();

            Assert.IsTrue(json.Contains("\"rootProp\""));
            Assert.IsTrue(json.Contains("\"nestedProp\""));
        }

        [TestMethod]
        public void NamingPolicy_WithArrayElements_DoesNotAffectArrays()
        {
            var arr = new[] { new { PropName = "value" } };
            var options = new JsonOptions { NamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonValue.Serialize(arr, options).ToString();

            Assert.IsTrue(json.Contains("\"propName\""));
        }
    }
}

using LightJson;
using LightJson.Converters;
using LightJson.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace LightJsonTests
{
    [TestClass]
    public class JsonConvertersTests
    {
        public class CustomType
        {
            public string? Value { get; set; }
        }

        public class CustomTypeConverter : JsonConverter
        {
            public override bool CanSerialize(Type type, JsonOptions options)
            {
                return type == typeof(CustomType);
            }

            public override JsonValue Serialize(object value, JsonOptions options)
            {
                var obj = (CustomType)value;
                return new JsonValue("CUSTOM_" + obj.Value);
            }

            public override object Deserialize(JsonValue value, Type requestedType, JsonOptions options)
            {
                var str = value.GetString();
                return new CustomType { Value = str.Substring(7) };
            }
        }

        [TestMethod]
        public void CustomConverter_Serialize_UsesConverter()
        {
            var obj = new CustomType { Value = "Test" };
            var options = new JsonOptions();
            options.Converters.Add(new CustomTypeConverter());

            var jsonVal = JsonValue.Serialize(obj, options);
            Assert.AreEqual("CUSTOM_Test", jsonVal.GetString());
        }

        [TestMethod]
        public void CustomConverter_Deserialize_UsesConverter()
        {
            var json = "\"CUSTOM_Test\"";
            var options = new JsonOptions();
            options.Converters.Add(new CustomTypeConverter());

            var obj = options.Deserialize(json).Get<CustomType>();
            Assert.AreEqual("Test", obj.Value);
        }

        [TestMethod]
        public void CustomConverter_RoundTrip_PreservesData()
        {
            var obj = new CustomType { Value = "RoundTrip" };
            var options = new JsonOptions();
            options.Converters.Add(new CustomTypeConverter());

            var json = JsonValue.Serialize(obj, options).ToString();
            var deserialized = options.Deserialize(json).Get<CustomType>();
            Assert.AreEqual(obj.Value, deserialized.Value);
        }

        public class CustomDateTimeConverter : JsonConverter
        {
            public override bool CanSerialize(Type type, JsonOptions options)
            {
                return type == typeof(DateTime);
            }

            public override JsonValue Serialize(object value, JsonOptions options)
            {
                return new JsonValue("CUSTOM_DATE");
            }

            public override object Deserialize(JsonValue value, Type requestedType, JsonOptions options)
            {
                return DateTime.MinValue;
            }
        }

        [TestMethod]
        public void ConverterPrecedence_CustomOverridesBuiltIn()
        {
            var dt = DateTime.UtcNow;
            var options = new JsonOptions();
            options.Converters.Add(new CustomDateTimeConverter());

            var jsonVal = JsonValue.Serialize(dt, options);
            Assert.AreEqual("CUSTOM_DATE", jsonVal.GetString());
        }

        public class OptionsCheckingConverter : JsonConverter
        {
            public bool ReceivedOptions { get; private set; }

            public override bool CanSerialize(Type type, JsonOptions options)
            {
                ReceivedOptions = options != null;
                return type == typeof(CustomType);
            }

            public override JsonValue Serialize(object value, JsonOptions options)
            {
                ReceivedOptions = options != null;
                return JsonValue.Null;
            }

            public override object Deserialize(JsonValue value, Type requestedType, JsonOptions options)
            {
                ReceivedOptions = options != null;
                return new CustomType();
            }
        }

        [TestMethod]
        public void ConverterWithOptions_ReceivesOptions()
        {
            var converter = new OptionsCheckingConverter();
            var options = new JsonOptions();
            options.Converters.Add(converter);

            JsonValue.Serialize(new CustomType(), options);
            Assert.IsTrue(converter.ReceivedOptions);
        }

        public class ThrowingConverter : JsonConverter
        {
            public override bool CanSerialize(Type type, JsonOptions options) => type == typeof(CustomType);

            public override JsonValue Serialize(object value, JsonOptions options)
            {
                throw new InvalidOperationException("Converter failed");
            }

            public override object Deserialize(JsonValue value, Type requestedType, JsonOptions options)
            {
                throw new InvalidOperationException("Converter failed");
            }
        }

        [TestMethod]
        public void ConverterThrowsException_PropagatesException()
        {
            var options = new JsonOptions();
            options.Converters.Add(new ThrowingConverter());

            // JsonValue.Serialize wraps exceptions in JsonException?
            // Let's check.
            Assert.ThrowsException<JsonException>(() => JsonValue.Serialize(new CustomType(), options));
        }
    }
}

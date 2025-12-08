using LightJson;
using LightJson.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LightJsonTests
{
    [TestClass]
    public class NumbersAsStringsTests
    {
        private class NumericHolder
        {
            public int IntValue { get; set; }
            public double DoubleValue { get; set; }
        }

        [TestMethod]
        public void AllowNumbersAsStrings_True_ParsesStringAsNumber()
        {
            var json = "{\"IntValue\": \"123\"}";
            var options = new JsonOptions { AllowNumbersAsStrings = true };
            var holder = options.Deserialize(json).Get<NumericHolder>();
            Assert.AreEqual(123, holder.IntValue);
        }

        [TestMethod]
        public void AllowNumbersAsStrings_False_RejectsStringAsNumber()
        {
            var json = "{\"IntValue\": \"123\"}";
            var options = new JsonOptions { AllowNumbersAsStrings = false };
            // Get<NumericHolder> calls GetNumber() which throws InvalidCastException if not allowed
            Assert.ThrowsException<InvalidCastException>(() => options.Deserialize(json).Get<NumericHolder>());
        }

        [TestMethod]
        public void AllowNumbersAsStrings_ParsesInteger_FromString()
        {
            var json = "{\"IntValue\": \"-42\"}";
            var options = new JsonOptions { AllowNumbersAsStrings = true };
            var holder = options.Deserialize(json).Get<NumericHolder>();
            Assert.AreEqual(-42, holder.IntValue);
        }

        [TestMethod]
        public void AllowNumbersAsStrings_ParsesDouble_FromString()
        {
            var json = "{\"DoubleValue\": \"12.34\"}";
            var options = new JsonOptions { AllowNumbersAsStrings = true };
            var holder = options.Deserialize(json).Get<NumericHolder>();
            Assert.AreEqual(12.34, holder.DoubleValue, 0.0001);
        }

        [TestMethod]
        public void AllowNumbersAsStrings_ParsesNegative_FromString()
        {
            var json = "{\"DoubleValue\": \"-456.78\"}";
            var options = new JsonOptions { AllowNumbersAsStrings = true };
            var holder = options.Deserialize(json).Get<NumericHolder>();
            Assert.AreEqual(-456.78, holder.DoubleValue, 0.0001);
        }

        [TestMethod]
        public void AllowNumbersAsStrings_ParsesScientificNotation_FromString()
        {
            var json = "{\"DoubleValue\": \"1.23e2\"}";
            var options = new JsonOptions { AllowNumbersAsStrings = true };
            var holder = options.Deserialize(json).Get<NumericHolder>();
            Assert.AreEqual(123.0, holder.DoubleValue, 0.0001);
        }

        [TestMethod]
        public void AllowNumbersAsStrings_InvalidString_ThrowsException()
        {
            var json = "{\"IntValue\": \"not a number\"}";
            var options = new JsonOptions { AllowNumbersAsStrings = true };
            // double.Parse throws FormatException
            Assert.ThrowsException<FormatException>(() => options.Deserialize(json).Get<NumericHolder>());
        }

        [TestMethod]
        public void AllowNumbersAsStrings_EmptyString_ThrowsException()
        {
            var json = "{\"IntValue\": \"\"}";
            var options = new JsonOptions { AllowNumbersAsStrings = true };
            Assert.ThrowsException<FormatException>(() => options.Deserialize(json).Get<NumericHolder>());
        }

        [TestMethod]
        public void AllowNumbersAsStrings_WhitespaceString_ThrowsException()
        {
            var json = "{\"IntValue\": \"   \"}";
            var options = new JsonOptions { AllowNumbersAsStrings = true };
            Assert.ThrowsException<FormatException>(() => options.Deserialize(json).Get<NumericHolder>());
        }
    }
}

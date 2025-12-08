using LightJson;
using LightJson.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;

namespace LightJsonTests
{
    [TestClass]
    public class PrimitiveConvertersTests
    {
        private class Holder<T>
        {
            public T? Value { get; set; }
        }

        [TestMethod]
        public void ByteArrayConverter_RoundTrip_PreservesBytes()
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5 };
            var holder = new Holder<byte[]> { Value = bytes };
            var json = JsonValue.Serialize(holder).ToString();
            var deserialized = JsonValue.Parse(json, null).Get<Holder<byte[]>>();
            CollectionAssert.AreEqual(bytes, deserialized.Value);
        }

        [TestMethod]
        public void GuidConverter_RoundTrip_PreservesGuid()
        {
            var guid = Guid.NewGuid();
            var holder = new Holder<Guid> { Value = guid };
            var json = JsonValue.Serialize(holder).ToString();
            var deserialized = JsonValue.Parse(json, null).Get<Holder<Guid>>();
            Assert.AreEqual(guid, deserialized.Value);
        }

        [TestMethod]
        public void DateTimeConverter_RoundTrip_PreservesDateTime()
        {
            // Default format "s" does not include fractional seconds.
            var dt = new DateTime(2023, 10, 27, 14, 30, 0, DateTimeKind.Utc);
            var holder = new Holder<DateTime> { Value = dt };
            var json = JsonValue.Serialize(holder).ToString();
            var deserialized = JsonValue.Parse(json, null).Get<Holder<DateTime>>();
            Assert.AreEqual(dt, deserialized.Value);
        }

        [TestMethod]
        public void TimeSpanConverter_RoundTrip_PreservesTimeSpan()
        {
            var ts = TimeSpan.FromMinutes(123);
            var holder = new Holder<TimeSpan> { Value = ts };
            var json = JsonValue.Serialize(holder).ToString();
            var deserialized = JsonValue.Parse(json, null).Get<Holder<TimeSpan>>();
            Assert.AreEqual(ts, deserialized.Value);
        }

        [TestMethod]
        public void DateOnlyConverter_RoundTrip_PreservesDateOnly()
        {
            var d = new DateOnly(2023, 10, 27);
            var holder = new Holder<DateOnly> { Value = d };
            var json = JsonValue.Serialize(holder).ToString();
            var deserialized = JsonValue.Parse(json, null).Get<Holder<DateOnly>>();
            Assert.AreEqual(d, deserialized.Value);
        }

        [TestMethod]
        public void TimeOnlyConverter_RoundTrip_PreservesTimeOnly()
        {
            var t = new TimeOnly(14, 30, 0);
            var holder = new Holder<TimeOnly> { Value = t };
            var json = JsonValue.Serialize(holder).ToString();
            var deserialized = JsonValue.Parse(json, null).Get<Holder<TimeOnly>>();
            Assert.AreEqual(t, deserialized.Value);
        }

        [TestMethod]
        public void CharConverter_RoundTrip_PreservesChar()
        {
            var c = 'A';
            var holder = new Holder<char> { Value = c };
            var json = JsonValue.Serialize(holder).ToString();
            var deserialized = JsonValue.Parse(json, null).Get<Holder<char>>();
            Assert.AreEqual(c, deserialized.Value);
        }

        [TestMethod]
        public void UriConverter_RoundTrip_PreservesUri()
        {
            var uri = new Uri("https://example.com/path?query=1");
            var holder = new Holder<Uri> { Value = uri };
            var json = JsonValue.Serialize(holder).ToString();
            var deserialized = JsonValue.Parse(json, null).Get<Holder<Uri>>();
            Assert.AreEqual(uri, deserialized.Value);
        }

        [TestMethod]
        public void IpAddressConverter_RoundTrip_PreservesIpAddress()
        {
            var ip = IPAddress.Parse("192.168.1.1");
            var holder = new Holder<IPAddress> { Value = ip };
            var json = JsonValue.Serialize(holder).ToString();
            var deserialized = JsonValue.Parse(json, null).Get<Holder<IPAddress>>();
            Assert.AreEqual(ip, deserialized.Value);
        }
    }
}

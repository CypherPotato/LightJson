using LightJson;
using LightJson.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace LightJsonTests
{
    [TestClass]
    public class JsonToonWriterTests
    {
        [TestMethod]
        public void Toon_Unwrap_Json_Mixed_Structure()
        {
            // Arrange
            var json = """
                {
                  "context": {
                    "task": "Our favorite hikes together",
                    "location": "Boulder",
                    "season": "spring_2025"
                  },
                  "friends": [
                    "ana",
                    "luis",
                    "sam"
                  ],
                  "hikes": [
                    {
                      "id": 1,
                      "name": "Blue Lake Trail",
                      "distanceKm": 7.5,
                      "elevationGain": 320,
                      "companion": "ana",
                      "wasSunny": true
                    },
                    {
                      "id": 2,
                      "name": "Ridge Overlook",
                      "distanceKm": 9.2,
                      "elevationGain": 540,
                      "companion": "luis",
                      "wasSunny": false
                    },
                    {
                      "id": 3,
                      "name": "Wildflower Loop",
                      "distanceKm": 5.1,
                      "elevationGain": 180,
                      "companion": "sam",
                      "wasSunny": true
                    }
                  ]
                }
                """;
            var expectedToon = """
                context:
                  task: Our favorite hikes together
                  location: Boulder
                  season: spring_2025
                friends[3]: ana,luis,sam
                hikes[3]{id,name,distanceKm,elevationGain,companion,wasSunny}:
                  1,Blue Lake Trail,7.5,320,ana,true
                  2,Ridge Overlook,9.2,540,luis,false
                  3,Wildflower Loop,5.1,180,sam,true
                """;

            // Act
            var jsonValue = JsonValue.Deserialize(json);
            var result = SerializeToToon(jsonValue);

            // Assert
            Assert.AreEqual(expectedToon, result);
        }

        [TestMethod]
        public void Toon_Unwrap_Json_Nested_Objects()
        {
            // Arrange
            var json = """
                {
                  "orders": [
                    {
                      "orderId": "ORD-001",
                      "customer": {
                        "name": "Alice Chen",
                        "email": "alice@example.com"
                      },
                      "items": [
                        {
                          "sku": "WIDGET-A",
                          "quantity": 2,
                          "price": 29.99
                        },
                        {
                          "sku": "GADGET-B",
                          "quantity": 1,
                          "price": 49.99
                        }
                      ],
                      "total": 109.97,
                      "status": "shipped"
                    },
                    {
                      "orderId": "ORD-002",
                      "customer": {
                        "name": "Bob Smith",
                        "email": "bob@example.com"
                      },
                      "items": [
                        {
                          "sku": "THING-C",
                          "quantity": 3,
                          "price": 15
                        }
                      ],
                      "total": 45,
                      "status": "delivered"
                    }
                  ]
                }
                """;
            var expectedToon = """
                orders[2]:
                - orderId: ORD-001
                  customer:
                    name: Alice Chen
                    email: alice@example.com
                  items[2]{sku,quantity,price}:
                    WIDGET-A,2,29.99
                    GADGET-B,1,49.99
                  total: 109.97
                  status: shipped
                - orderId: ORD-002
                  customer:
                    name: Bob Smith
                    email: bob@example.com
                  items[1]{sku,quantity,price}:
                    THING-C,3,15
                  total: 45
                  status: delivered
                """;

            // Act
            var jsonValue = JsonValue.Deserialize(json);
            var result = SerializeToToon(jsonValue);

            // Assert
            Assert.AreEqual(expectedToon, result);
        }

        [TestMethod]
        public void Toon_Unwrap_Json_Tabular_Data()
        {
            // Arrange
            var json = """
                {
                  "metrics": [
                    {
                      "date": "2025-01-01",
                      "views": 5200,
                      "clicks": 180,
                      "conversions": 24,
                      "revenue": 2890.5
                    },
                    {
                      "date": "2025-01-02",
                      "views": 6100,
                      "clicks": 220,
                      "conversions": 31,
                      "revenue": 3450
                    },
                    {
                      "date": "2025-01-03",
                      "views": 4800,
                      "clicks": 165,
                      "conversions": 19,
                      "revenue": 2100.25
                    },
                    {
                      "date": "2025-01-04",
                      "views": 5900,
                      "clicks": 205,
                      "conversions": 28,
                      "revenue": 3200
                    }
                  ]
                }
                """;

            var expectedToon = """
                metrics[4]{date,views,clicks,conversions,revenue}:
                  2025-01-01,5200,180,24,2890.5
                  2025-01-02,6100,220,31,3450
                  2025-01-03,4800,165,19,2100.25
                  2025-01-04,5900,205,28,3200
                """;

            // Act
            var jsonValue = JsonValue.Deserialize(json);
            var result = SerializeToToon(jsonValue);

            // Assert
            Assert.AreEqual(expectedToon, result);
        }

        [TestMethod]
        public void Toon_Unwrap_Json_Semi_Uniform_Data()
        {
            // Arrange
            var json = """
                {
                  "logs": [
                    {
                      "timestamp": "2025-01-15T10:23:45Z",
                      "level": "info",
                      "endpoint": "/api/users",
                      "statusCode": 200,
                      "responseTime": 45
                    },
                    {
                      "timestamp": "2025-01-15T10:24:12Z",
                      "level": "error",
                      "endpoint": "/api/orders",
                      "statusCode": 500,
                      "responseTime": 120,
                      "error": {
                        "message": "Database timeout",
                        "retryable": true
                      }
                    },
                    {
                      "timestamp": "2025-01-15T10:25:03Z",
                      "level": "info",
                      "endpoint": "/api/products",
                      "statusCode": 200,
                      "responseTime": 32
                    },
                    {
                      "timestamp": "2025-01-15T10:26:47Z",
                      "level": "warn",
                      "endpoint": "/api/payment",
                      "statusCode": 429,
                      "responseTime": 5,
                      "error": {
                        "message": "Rate limit exceeded",
                        "retryable": true
                      }
                    }
                  ]
                }
                """;

            var expectedToon = """
                logs[4]:
                - timestamp: "2025-01-15T10:23:45Z"
                  level: info
                  endpoint: /api/users
                  statusCode: 200
                  responseTime: 45
                - timestamp: "2025-01-15T10:24:12Z"
                  level: error
                  endpoint: /api/orders
                  statusCode: 500
                  responseTime: 120
                  error:
                    message: Database timeout
                    retryable: true
                - timestamp: "2025-01-15T10:25:03Z"
                  level: info
                  endpoint: /api/products
                  statusCode: 200
                  responseTime: 32
                - timestamp: "2025-01-15T10:26:47Z"
                  level: warn
                  endpoint: /api/payment
                  statusCode: 429
                  responseTime: 5
                  error:
                    message: Rate limit exceeded
                    retryable: true
                """;

            // Act
            var jsonValue = JsonValue.Deserialize(json);
            var result = SerializeToToon(jsonValue);

            // Assert
            Assert.AreEqual(expectedToon, result);
        }

        [TestMethod]
        public void Toon_Unwrap_Json_Complex_Object()
        {
            // Arrange
            var json = """
                {
                    "glossary": {
                        "title": "example glossary",
                		"GlossDiv": {
                            "title": "S",
                			"GlossList": {
                                "GlossEntry": {
                                    "ID": "SGML",
                					"SortAs": "SGML",
                					"GlossTerm": "Standard Generalized Markup Language",
                					"Acronym": "SGML",
                					"Abbrev": "ISO 8879:1986",
                					"GlossDef": {
                                        "para": "A meta-markup language, used to create markup languages such as DocBook.",
                						"GlossSeeAlso": ["GML", "XML"]
                                    },
                					"GlossSee": "markup"
                                }
                            }
                        }
                    }
                }
                """;

            var expectedToon = """
                glossary:
                  title: example glossary
                  GlossDiv:
                    title: S
                    GlossList:
                      GlossEntry:
                        ID: SGML
                        SortAs: SGML
                        GlossTerm: Standard Generalized Markup Language
                        Acronym: SGML
                        Abbrev: "ISO 8879:1986"
                        GlossDef:
                          para: "A meta-markup language, used to create markup languages such as DocBook."
                          GlossSeeAlso[2]: GML,XML
                        GlossSee: markup
                """;

            // Act
            var jsonValue = JsonValue.Deserialize(json);
            var result = SerializeToToon(jsonValue);

            // Assert
            Assert.AreEqual(expectedToon, result);
        }

        [TestMethod]
        public void Toon_Keyfolding_BaseTest()
        {
            // Arrange
            var json = """
                {"a": {"b": {"c":1}}}
                """;

            var expectedToon = """
                a.b.c: 1
                """;

            // Act
            using (var st = new StringWriter())
            using (var tw = new JsonToonWriter(st) { KeyFolding = ToonKeyFolding.Safe, FlattenDepth = 3 })
            {
                tw.Write(JsonValue.Deserialize(json));
                Assert.AreEqual(expectedToon, st.ToString());
            }
        }

        [TestMethod]
        public void Toon_KeyFolding_Test()
        {
            var json = """
                {
                  "server": {
                    "interface": "eth0",
                    "port": 80,
                    "ssl": {
                      "enabled": true,
                      "cert": "path/to/cert"
                    }
                  },
                  "logging": {
                    "level": "debug"
                  }
                }
                """;

            var jsonValue = JsonValue.Deserialize(json);

            // Case 1: Off (Default)
            using (var sw = new StringWriter())
            using (var writer = new JsonToonWriter(sw) { NewLine = "\n" })
            {
                writer.KeyFolding = ToonKeyFolding.Off;
                writer.Write(jsonValue);
                var result = sw.ToString();
                var expected = """
                    server:
                      interface: eth0
                      port: 80
                      ssl:
                        enabled: true
                        cert: path/to/cert
                    logging:
                      level: debug
                    """;
                Assert.AreEqual(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
            }

            // Case 2: Safe, Depth 2
            using (var sw = new StringWriter())
            using (var writer = new JsonToonWriter(sw) { NewLine = "\n" })
            {
                writer.KeyFolding = ToonKeyFolding.Safe;
                writer.FlattenDepth = 2;
                writer.Write(jsonValue);
                var result = sw.ToString();
                var expected = """
                    server.interface: eth0
                    server.port: 80
                    server.ssl.enabled: true
                    server.ssl.cert: path/to/cert
                    logging.level: debug
                    """;
                Assert.AreEqual(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
            }

            // Case 3: Safe, Depth 1
            using (var sw = new StringWriter())
            using (var writer = new JsonToonWriter(sw) { NewLine = "\n" })
            {
                writer.KeyFolding = ToonKeyFolding.Safe;
                writer.FlattenDepth = 1;
                writer.Write(jsonValue);
                var result = sw.ToString();
                var expected = """
                    server.interface: eth0
                    server.port: 80
                    server.ssl:
                      enabled: true
                      cert: path/to/cert
                    logging.level: debug
                    """;
                Assert.AreEqual(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
            }

            // Case 4: List Item Folding
            var jsonList = """
                [
                  {
                    "id": 1,
                    "details": {
                      "name": "Item 1",
                      "active": true
                    }
                  }
                ]
                """;
            var jsonListValue = JsonValue.Deserialize(jsonList);

            using (var sw = new StringWriter())
            using (var writer = new JsonToonWriter(sw) { NewLine = "\n" })
            {
                writer.KeyFolding = ToonKeyFolding.Safe;
                writer.FlattenDepth = 2;
                writer.Write(jsonListValue);
                var result = sw.ToString();
                var expected = """
                    [1]:
                    - id: 1
                      details.name: Item 1
                      details.active: true
                    """;
                Assert.AreEqual(expected.Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
            }
        }

        private static string SerializeToToon(JsonValue jsonValue)
        {
            using var stringWriter = new StringWriter();
            using var toonWriter = new JsonToonWriter(stringWriter);
            toonWriter.Write(jsonValue);

            return stringWriter.ToString();
        }
    }
}

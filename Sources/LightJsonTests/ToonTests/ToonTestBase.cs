using LightJson;
using LightJson.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Collections.Generic;

namespace LightJsonTests.ToonTests
{
    public class ToonTestBase
    {
        protected void RunEncodeTests(string fixtureFileName)
        {
            var workspaceRoot = FindWorkspaceRoot();
            var fixturePath = Path.Combine(workspaceRoot, "Sources", "LightJsonTests", "ToonTests", "fixtures", "encode", fixtureFileName);

            if (!File.Exists(fixturePath))
            {
                Assert.Inconclusive($"Fixture file not found: {fixturePath}. WorkspaceRoot detected as: {workspaceRoot}");
            }

            var jsonContent = File.ReadAllText(fixturePath);
            var fixture = JsonValue.Deserialize(jsonContent);
            var tests = fixture["tests"].GetJsonArray();

            foreach (var test in tests)
            {
                var testName = test["name"].GetString();
                var input = test["input"];
                var expected = test["expected"].GetString();

                var indentSize = 2;
                var delimiter = ',';
                var keyFolding = ToonKeyFolding.Off;
                var flattenDepth = 8;

                var options = test["options"];
                if (options.IsDefined)
                {
                    if (options["indent"].IsDefined)
                    {
                        indentSize = options["indent"].GetInteger();
                    }
                    if (options["delimiter"].IsDefined)
                    {
                        var dStr = options["delimiter"].GetString();
                        if (!string.IsNullOrEmpty(dStr))
                        {
                            delimiter = dStr[0];
                        }
                    }
                    if (options["keyFolding"].IsDefined)
                    {
                        var kf = options["keyFolding"].GetString();
                        if (kf == "safe")
                        {
                            keyFolding = ToonKeyFolding.Safe;
                        }
                    }
                    if (options["flattenDepth"].IsDefined)
                    {
                        flattenDepth = options["flattenDepth"].GetInteger();
                    }
                }

                try
                {
                    var result = SerializeToToon(input, indentSize, delimiter, keyFolding, flattenDepth);

                    // Normalize line endings for comparison
                    var normalizedExpected = expected.Replace("\r\n", "\n");
                    var normalizedResult = result.Replace("\r\n", "\n");

                    Assert.AreEqual(normalizedExpected, normalizedResult, $"Test '{testName}' failed.");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Test '{testName}' failed with exception: {ex.Message}");
                }
            }
        }

        private string SerializeToToon(JsonValue jsonValue, int indentSize, char delimiter, ToonKeyFolding keyFolding, int flattenDepth)
        {
            using var stringWriter = new StringWriter();
            // Use \n for newlines to match fixtures usually
            using var toonWriter = new JsonToonWriter(stringWriter)
            {
                NewLine = "\n",
                IndentSize = indentSize,
                Delimiter = delimiter,
                KeyFolding = keyFolding,
                FlattenDepth = flattenDepth
            };
            toonWriter.Write(jsonValue);

            return stringWriter.ToString();
        }

        private string FindWorkspaceRoot()
        {
            var current = AppDomain.CurrentDomain.BaseDirectory;
            while (current != null)
            {
                if (Directory.Exists(Path.Combine(current, "Sources")))
                {
                    return current;
                }
                if (File.Exists(Path.Combine(current, "LightJson.sln")))
                {
                    return Directory.GetParent(current)?.FullName ?? current;
                }
                current = Directory.GetParent(current)?.FullName;
            }
            return ".";
        }
    }
}

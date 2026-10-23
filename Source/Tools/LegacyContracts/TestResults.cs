// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using System.Xml.Linq;

namespace Cratis.Chronicle.Tools.LegacyContracts;

static class TestResults
{
    public static IReadOnlyList<TestResult> Read(string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new CannotEvaluate($"TRX directory '{directory}' does not exist.");
        }

        var paths = Directory.GetFiles(directory, "*.trx", SearchOption.AllDirectories);
        if (paths.Length == 0)
        {
            throw new CannotEvaluate("No TRX files were supplied.");
        }

        // A project whose specs are all filtered out writes a TRX without results; only the aggregate must be non-empty.
        return RequireAny(paths.SelectMany(_ => ParseDocument(File.ReadAllText(_))).ToArray());
    }

    public static IReadOnlyList<TestResult> Parse(string xml) => RequireAny(ParseDocument(xml));

    static IReadOnlyList<TestResult> RequireAny(IReadOnlyList<TestResult> results) =>
        results.Count > 0 ? results : throw new CannotEvaluate("TRX contains no test results.");

    static TestResult[] ParseDocument(string xml)
    {
        using var text = new StringReader(xml);
        using var reader = XmlReader.Create(text, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        var document = XDocument.Load(reader);
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        if (document.Root?.Name != ns + "TestRun")
        {
            throw new CannotEvaluate("Not a TRX TestRun.");
        }

        var definitions = document.Descendants(ns + "UnitTest").ToDictionary(
            _ => Required(_, "id"),
            _ => FullName(_.Element(ns + "TestMethod") ?? throw new CannotEvaluate("TRX test has no TestMethod.")),
            StringComparer.Ordinal);
        var results = document.Descendants(ns + "UnitTestResult").Select(result =>
        {
            var id = Required(result, "testId");
            if (!definitions.TryGetValue(id, out var name))
            {
                throw new CannotEvaluate($"TRX result '{id}' has no test definition.");
            }

            return new TestResult(name, string.Equals(Required(result, "outcome"), "Passed", StringComparison.Ordinal));
        }).ToArray();

        return results;
    }

    static string FullName(XElement method) => $"{Required(method, "className")}.{Required(method, "name")}";

    static string Required(XElement element, string attribute) =>
        (string?)element.Attribute(attribute) is { Length: > 0 } value ? value : throw new CannotEvaluate($"TRX is missing '{attribute}'.");
}

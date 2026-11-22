// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// Extracts physical operators and row counts from SQL Server's actual showplan namespace.
/// </summary>
internal static class SqlServerPlanSummary
{
    static readonly XNamespace _showplan = "http://schemas.microsoft.com/sqlserver/2004/07/showplan";

    /// <summary>
    /// Summarizes all operators, summing runtime counters across parallel threads.
    /// </summary>
    /// <param name="xml">The full actual showplan XML.</param>
    /// <returns>Physical operators, estimated/actual rows, indexes and query timings.</returns>
    public static string Format(string xml)
    {
        var document = XDocument.Parse(xml);
        var operators = document.Descendants(_showplan + "RelOp").Select(Operator);
        var times = document.Descendants(_showplan + "QueryTimeStats").Select(time =>
            $"Query CPU={Value(time, "CpuTime")} ms; elapsed={Value(time, "ElapsedTime")} ms");
        return string.Join('\n', operators.Concat(times));
    }

    static string Operator(XElement operation)
    {
        // Only this operator's counters and object, not those belonging to nested RelOps.
        var children = operation.Descendants().Where(element => element.Ancestors(_showplan + "RelOp").First() == operation).ToArray();
        var counters = children.Where(element => element.Name == _showplan + "RunTimeCountersPerThread").ToArray();
        var objects = children.Where(element => element.Name == _showplan + "Object").Select(element =>
            $"{Value(element, "Table")}.{Value(element, "Index")}");
        var lookup = children.Any(element =>
            string.Equals((string?)element.Attribute("Lookup"), "1", StringComparison.Ordinal) ||
            string.Equals((string?)element.Attribute("Lookup"), "true", StringComparison.Ordinal)) ? " (Key Lookup)" : string.Empty;
        return $"Node {Value(operation, "NodeId")}: {Value(operation, "PhysicalOp")}{lookup}; estimated rows={Value(operation, "EstimateRows")}; actual rows={Total(counters, "ActualRows")}; executions={Total(counters, "ActualExecutions")}; {string.Join(", ", objects)}";
    }

    static string Value(XElement element, string attribute) => (string?)element.Attribute(attribute) ?? "not reported";

    static string Total(XElement[] counters, string attribute) => counters.Length == 0
        ? "not reported"
        : counters.Sum(counter => double.Parse(Value(counter, attribute), CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture);
}

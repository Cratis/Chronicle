// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

/// <summary>
/// Planted payload changes proving the gate detects breaks and allows additions.
/// </summary>
public static class PayloadSelfTest
{
    const string Baseline = "concept Cratis.Id : string\n"
        + "custom Cratis.Path\n"
        + "enum Cratis.Status : int\n  Active = 1\n  Paused = 2\n"
        + "json Cratis.Payload\n  name: string\n"
        + "oneof Cratis.Result\n  case0: string\n  case1: int\n"
        + "orleans [Alias(\"generated\")] Cratis.Generated\n  [Id(0)] Name: string\n";

    /// <summary>
    /// Gets the planted cases and their expected break kinds.
    /// </summary>
    public static IReadOnlyList<(string Name, string Current, ContractBreakKind? Expected)> Cases { get; } =
    [
        ("property added", Baseline.Replace("  name: string", "  name: string\n  age: int", StringComparison.Ordinal), null),
        ("enum member added", Baseline.Replace("  Paused = 2", "  Paused = 2\n  Stopped = 3", StringComparison.Ordinal), null),
        ("type added", Baseline + "\njson Cratis.Added", null),
        ("property removed", Baseline.Replace("  name: string\n", string.Empty, StringComparison.Ordinal), ContractBreakKind.PayloadPropertyRemoved),
        ("property renamed", Baseline.Replace("name:", "renamed:", StringComparison.Ordinal), ContractBreakKind.PayloadPropertyRemoved),
        ("property changed type", Baseline.Replace("name: string", "name: int", StringComparison.Ordinal), ContractBreakKind.PayloadPropertyTypeChanged),
        ("enum member removed", Baseline.Replace("  Paused = 2\n", string.Empty, StringComparison.Ordinal), ContractBreakKind.EnumMemberRemoved),
        ("enum value changed", Baseline.Replace("Active = 1", "Active = 3", StringComparison.Ordinal), ContractBreakKind.EnumValueChanged),
        ("type moved", Baseline.Replace("Cratis.Payload", "Cratis.Moved.Payload", StringComparison.Ordinal), ContractBreakKind.PayloadTypeRemoved),
        ("aliased type moved", Baseline.Replace("Cratis.Generated", "Cratis.Moved.Generated", StringComparison.Ordinal), null),
        ("serializer ID changed", Baseline.Replace("Id(0)", "Id(1)", StringComparison.Ordinal), ContractBreakKind.PayloadPropertyRemoved),
        ("serializer member renamed", Baseline.Replace("Name:", "Renamed:", StringComparison.Ordinal), ContractBreakKind.PayloadPropertyRemoved),
        ("concept underlying changed", Baseline.Replace("Cratis.Id : string", "Cratis.Id : int", StringComparison.Ordinal), ContractBreakKind.PayloadKindChanged),
        ("enum underlying changed", Baseline.Replace("Cratis.Status : int", "Cratis.Status : long", StringComparison.Ordinal), ContractBreakKind.PayloadKindChanged),
        ("codec changed", Baseline.Replace("json Cratis.Payload\n  name: string", "orleans Cratis.Payload\n  [Id(0)] Name: string", StringComparison.Ordinal), ContractBreakKind.PayloadKindChanged),
        ("OneOf case reordered", Baseline.Replace("case0: string\n  case1: int", "case0: int\n  case1: string", StringComparison.Ordinal), ContractBreakKind.PayloadPropertyTypeChanged),
        ("OneOf case removed", Baseline.Replace("  case1: int\n", string.Empty, StringComparison.Ordinal), ContractBreakKind.PayloadPropertyRemoved),
        ("custom type removed", Baseline.Replace("custom Cratis.Path\n", string.Empty, StringComparison.Ordinal), ContractBreakKind.PayloadTypeRemoved)
    ];

    /// <summary>
    /// Runs every planted case through the production parser and comparison.
    /// </summary>
    /// <param name="writer">Where to report results.</param>
    /// <returns>The number of cases that did not produce the expected result.</returns>
    public static int Run(TextWriter writer)
    {
        var baseline = PayloadContractManifest.Parse(Baseline);
        var failures = 0;
        foreach (var (name, current, expected) in Cases)
        {
            var breaks = PayloadContractComparison.Compare(baseline, PayloadContractManifest.Parse(current));
            var passed = expected is null ? breaks.Count == 0 : breaks.Count > 0 && breaks.All(_ => _.Kind == expected);
            if (!passed)
            {
                failures++;
            }

            writer.WriteLine($"{(passed ? "ok" : "FAIL")} {name}");
        }

        writer.WriteLine($"{Cases.Count - failures} of {Cases.Count} planted payload cases passed.");
        return failures;
    }
}

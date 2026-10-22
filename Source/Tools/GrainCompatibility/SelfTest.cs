// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility;

/// <summary>
/// Checks that the comparison still detects every kind of break, by planting each one in a known manifest.
/// </summary>
/// <remarks>
/// A compatibility gate that stopped detecting anything would pass every change. This is the run that proves it
/// still fails: each case plants exactly one break and must be reported as exactly that kind.
/// </remarks>
public static class SelfTest
{
    /// <summary>
    /// The baseline every case is planted in.
    /// </summary>
    public static readonly string Baseline = string.Join(
        '\n',
        "# OrleansContracts format: 2",
        "class [GrainType(\"thing\")] Example.Thing",
        string.Empty,
        "interface [GrainInterfaceType(\"Example.IThing\")] Example.IThing [Version(1)]",
        "  11111111: Do(int) -> Task",
        "  2: Get() -> Task<string>");

    /// <summary>
    /// Gets the cases: a name, the changed manifest, and the breaks it must produce.
    /// </summary>
    public static IReadOnlyList<(string Name, string Current, ContractBreakKind[] Expected)> Cases { get; } =
    [
        ("an unchanged manifest", Baseline, []),
        ("a method added with a higher version", Baseline.Replace("[Version(1)]", "[Version(2)]") + "\n  33333333: Added() -> Task", []),
        ("a new interface", Baseline + "\n\ninterface [GrainInterfaceType(\"Example.INew\")] Example.INew [Version(0)]\n  44444444: Do() -> Task", []),
        ("a method renamed in CLR only", Baseline.Replace("Do(int)", "DoIt(int)"), []),
        ("an interface removed", Baseline[..Baseline.IndexOf("interface", StringComparison.Ordinal)], [ContractBreakKind.InterfaceRemoved]),
        ("an interface retired", Baseline.Replace("interface [", "*RETIRED* interface ["), [ContractBreakKind.InterfaceRemoved]),
        ("a parameter added, which changes the identity", Baseline.Replace("11111111: Do(int)", "55555555: Do(int, string?)"), [ContractBreakKind.MethodRemoved, ContractBreakKind.MethodsAddedWithoutVersionIncrease]),
        ("a return type changed under a fixed identity", Baseline.Replace("Task<string>", "Task<int>"), [ContractBreakKind.MethodSignatureChanged]),
        ("a method added without a higher version", Baseline + "\n  33333333: Added() -> Task", [ContractBreakKind.MethodsAddedWithoutVersionIncrease]),
        ("a version lowered", Baseline.Replace("[Version(1)]", "[Version(0)]"), [ContractBreakKind.VersionDecreased]),
        ("a grain class removed", Baseline.Replace("class [GrainType(\"thing\")] Example.Thing", string.Empty), [ContractBreakKind.GrainClassRemoved]),
        ("a grain type changed", Baseline.Replace("\"thing\"", "\"thing2\""), [ContractBreakKind.GrainClassRemoved])
    ];

    /// <summary>
    /// Runs every case.
    /// </summary>
    /// <param name="output">Where to report each case.</param>
    /// <returns>The number of cases that did not produce what they must.</returns>
    public static int Run(TextWriter output)
    {
        var baseline = ContractManifest.Parse(Baseline);
        var failures = 0;

        foreach (var (name, current, expected) in Cases)
        {
            var found = ContractComparison.Compare(baseline, ContractManifest.Parse(current)).Select(_ => _.Kind).Order().ToArray();
            var matches = found.SequenceEqual(expected.Order());
            output.WriteLine($"{(matches ? "ok  " : "FAIL")} {name}: expected [{string.Join(", ", expected)}], found [{string.Join(", ", found)}]");
            failures += matches ? 0 : 1;
        }

        output.WriteLine($"{Cases.Count - failures} of {Cases.Count} planted cases detected as expected.");
        return failures;
    }
}

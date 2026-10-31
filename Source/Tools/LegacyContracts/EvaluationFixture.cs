// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Tools.LegacyContracts;

record EvaluationFixture(Matrix Matrix, OwnerIssue[] Issues, IReadOnlyDictionary<string, JobResult> Jobs, string? Trx)
{
    public const string TestName = "Example.Contract.should_reject_a_break";
    public const string OtherTestName = "Example.Contract.should_keep_the_contract";
    public static string PassingTrx { get; } = TrxFor("Passed");

    public static EvaluationFixture Clean() => new(
        new("report", [new("W", "Contract remains compatible", [5222], "runnable", ["wire"], [new("Example.Contract", 1)], [new("removed field", TestName)], [])]),
        [new(5222, "OPEN", null, false)],
        new Dictionary<string, JobResult>(StringComparer.Ordinal) { ["wire"] = new("success", null) },
        PassingTrx);

    public EvaluationFixture WithRow(MatrixRow row) => this with { Matrix = Matrix with { Rows = [row] } };

    public EvaluationFixture WithJob(string result, bool skipped = false) => this with
    {
        Jobs = new Dictionary<string, JobResult>(StringComparer.Ordinal)
        {
            ["wire"] = new(result, new Dictionary<string, string>(StringComparer.Ordinal) { ["skipped"] = skipped ? "true" : "false" })
        }
    };

    public EvaluationRun Evaluate() => EvaluationRun.Read(
        JsonSerializer.Serialize(Matrix),
        JsonSerializer.Serialize(Issues),
        JsonSerializer.Serialize(Jobs),
        () => TestResults.Parse(Trx ?? throw new CannotEvaluate("No TRX files were supplied.")));

    public static string WithOtherTest(string outcome) => PassingTrx
        .Replace("</Results>", $"<UnitTestResult testId=\"test-2\" outcome=\"{outcome}\" /></Results>", StringComparison.Ordinal)
        .Replace("</TestDefinitions>", "<UnitTest id=\"test-2\"><TestMethod className=\"Example.Contract\" name=\"should_keep_the_contract\" /></UnitTest></TestDefinitions>", StringComparison.Ordinal);

    public static string TrxFor(string outcome) => string.Join(
        "\n",
        "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\">",
        "  <Results>",
        $"    <UnitTestResult testId=\"test-1\" outcome=\"{outcome}\" />",
        "  </Results>",
        "  <TestDefinitions>",
        "    <UnitTest id=\"test-1\"><TestMethod className=\"Example.Contract\" name=\"should_reject_a_break\" /></UnitTest>",
        "  </TestDefinitions>",
        "</TestRun>");
}

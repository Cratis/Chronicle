// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.LegacyContracts;

static class SelfTest
{
    public static IReadOnlyList<SelfTestCase> Cases { get; } = CreateCases();

    public static int Run(TextWriter output)
    {
        var failures = 0;
        foreach (var test in Cases)
        {
            var result = test.Evaluate();
            var matches = result.ExitCode == test.ExitCode && (test.Verdict is null || result.Rows.Single().Kind == test.Verdict);
            output.WriteLine($"{(matches ? "ok  " : "FAIL")} {test.Name}: expected {test.ExitCode}/{test.Verdict}, found {result.ExitCode}/{result.Rows.SingleOrDefault()?.Kind} {result.Error}");
            failures += matches ? 0 : 1;
        }

        output.WriteLine($"{Cases.Count - failures} of {Cases.Count} planted cases detected as expected.");
        return failures;
    }

    static IReadOnlyList<SelfTestCase> CreateCases()
    {
        var clean = EvaluationFixture.Clean();
        var row = clean.Matrix.Rows[0];
        var pending = clean.WithRow(row with { Status = "pending-owner", RequiredProofs = ["Prove refusal before adoption."] });
        return
        [
            Case("a_clean_row", clean, 0, RowVerdictKind.Green),
            Case("an_owner_is_missing", clean.WithRow(row with { Owners = [] }), 1, RowVerdictKind.Unowned),
            Case("the_owner_is_a_pull_request", clean with { Issues = [new(5222, "OPEN", null, true)] }, 1, RowVerdictKind.Unowned),
            Case("the_owner_was_not_planned", clean with { Issues = [new(5222, "CLOSED", "NOT_PLANNED", false)] }, 1, RowVerdictKind.Unowned),
            Case("the_owner_was_a_duplicate", clean with { Issues = [new(5222, "CLOSED", "DUPLICATE", false)] }, 1, RowVerdictKind.Unowned),
            Case("issue_lookup_failed", clean with { Issues = [] }, 2, null),
            Case("a_selected_test_failed", clean with { Trx = EvaluationFixture.TrxFor("Failed") }, 1, RowVerdictKind.Red),
            Case("a_selected_test_was_skipped", clean with { Trx = EvaluationFixture.TrxFor("NotExecuted") }, 1, RowVerdictKind.Red),
            Case("a_selector_matches_nothing", clean.WithRow(row with { Specs = [new("Missing.Contract", 1)] }), 1, RowVerdictKind.Red),
            Case("a_selector_is_below_minimum", clean.WithRow(row with { Specs = [new("Example.Contract", 2)] }), 1, RowVerdictKind.Red),
            Case("a_selector_only_matches_a_similar_prefix", clean.WithRow(row with { Specs = [new("Example.Contrac", 1)] }), 1, RowVerdictKind.Red),
            Case("a_planted_spec_is_missing", clean.WithRow(row with { PlantedDefects = [new("removed field", EvaluationFixture.OtherTestName)] }), 1, RowVerdictKind.Red),
            Case("a_pending_row_in_report_mode", pending, 0, RowVerdictKind.Pending),
            Case("a_pending_row_in_release_mode", pending with { Matrix = pending.Matrix with { Enforcement = "release" } }, 1, RowVerdictKind.Pending),
            Case("a_pending_row_is_stale", pending with { Issues = [new(5222, "CLOSED", "COMPLETED", false)] }, 1, RowVerdictKind.StalePending),
            Case("a_job_skipped_in_report_mode", clean.WithJob("skipped"), 0, RowVerdictKind.Skipped),
            Case("a_job_skipped_in_release_mode", clean.WithJob("skipped") with { Matrix = clean.Matrix with { Enforcement = "release" } }, 1, RowVerdictKind.Skipped),
            Case("a_successful_wire_job_skipped_its_comparison", clean.WithJob("success", skipped: true), 0, RowVerdictKind.Skipped),
            Case("a_required_job_failed", clean.WithJob("failure"), 1, RowVerdictKind.Red),
            Case("a_required_job_was_cancelled", clean.WithJob("cancelled"), 1, RowVerdictKind.Red),
            Case("a_job_result_is_missing", clean with { Jobs = new Dictionary<string, JobResult>(StringComparer.Ordinal) }, 2, null),
            Case("trx_is_absent", clean with { Trx = null }, 2, null),
            Case("trx_is_malformed", clean with { Trx = "<TestRun>" }, 2, null),
            Case("the_manifest_has_no_rows", clean with { Matrix = clean.Matrix with { Rows = [] } }, 2, null),
            Case("the_manifest_status_is_unknown", clean.WithRow(row with { Status = "done" }), 2, null),
            Case("the_manifest_minimum_is_zero", clean.WithRow(row with { Specs = [new("Example.Contract", 0)] }), 2, null),
            Case("a_completed_owner_of_a_runnable_row", clean with { Issues = [new(5222, "CLOSED", "COMPLETED", false)] }, 0, RowVerdictKind.Green),
            Case("one_pending_owner_remains_open", pending.WithRow(pending.Matrix.Rows[0] with { Owners = [5222, 5224] }) with { Issues = [new(5222, "CLOSED", "COMPLETED", false), new(5224, "OPEN", null, false)] }, 0, RowVerdictKind.Pending),
            Case("wire_skips_in_release_mode", clean.WithJob("success", skipped: true) with { Matrix = clean.Matrix with { Enforcement = "release" } }, 1, RowVerdictKind.Skipped),
            Case("duplicated_results_do_not_inflate_the_minimum", clean.WithRow(row with { Specs = [new("Example.Contract", 2)] }) with { Trx = EvaluationFixture.PassingTrx.Replace("</Results>", "<UnitTestResult testId=\"test-1\" outcome=\"Passed\" /></Results>", StringComparison.Ordinal) }, 1, RowVerdictKind.Red),
            Case("a_planted_spec_outside_the_selector_failed", clean.WithRow(row with { PlantedDefects = [new("independent planted break", EvaluationFixture.OtherTestName)] }) with { Trx = EvaluationFixture.WithOtherTest("Failed") }, 1, RowVerdictKind.Red),
            Case("trx_has_no_results", clean with { Trx = "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\" />" }, 2, null),
            Case("trx_has_no_definition_for_a_result", clean with { Trx = EvaluationFixture.PassingTrx.Replace("testId=\"test-1\"", "testId=\"unknown\"", StringComparison.Ordinal) }, 2, null),
            Case("a_job_result_is_invalid", clean.WithJob("unknown"), 2, null),
            Case("the_enforcement_mode_is_unknown", clean with { Matrix = clean.Matrix with { Enforcement = "ignore" } }, 2, null),
            Case("a_pending_row_has_no_required_proofs", pending.WithRow(pending.Matrix.Rows[0] with { RequiredProofs = [] }), 2, null),
            Case("a_runnable_row_has_no_jobs", clean.WithRow(row with { Jobs = [] }), 2, null),
            Case("a_runnable_row_has_no_planted_proofs", clean.WithRow(row with { PlantedDefects = [] }), 2, null),
            Case("row_ids_are_duplicated", clean with { Matrix = clean.Matrix with { Rows = [row, row] } }, 2, null),
            new("the_manifest_is_incomplete", () => EvaluationRun.Read("{}", "[]", "{}", () => []), 2, null),
            new("the_manifest_is_malformed", () => EvaluationRun.Read("{", "[]", "{}", () => []), 2, null)
        ];
    }

    static SelfTestCase Case(string name, EvaluationFixture fixture, int exitCode, RowVerdictKind? verdict) => new(name, fixture.Evaluate, exitCode, verdict);
}

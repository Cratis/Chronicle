// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.LegacyContracts;

static class MatrixEvaluation
{
    public static IReadOnlyList<RowVerdict> Evaluate(
        Matrix matrix,
        IReadOnlyList<OwnerIssue> issues,
        IReadOnlyDictionary<string, JobResult> jobs,
        IReadOnlyList<TestResult> tests)
    {
        if (issues.Any(_ => _ is null))
        {
            throw new CannotEvaluate("Issue input contains a null issue.");
        }

        return matrix.Rows.Select(row => EvaluateRow(row, issues, jobs, tests)).ToArray();
    }

    static RowVerdict EvaluateRow(
        MatrixRow row,
        IReadOnlyList<OwnerIssue> issues,
        IReadOnlyDictionary<string, JobResult> jobs,
        IReadOnlyList<TestResult> tests)
    {
        if (row.Owners.Length == 0)
        {
            return new(row.Id, RowVerdictKind.Unowned, 0, 0, "No owner issue.");
        }

        var owners = row.Owners.Select(number => FindOwner(number, issues)).ToArray();
        if (owners.Any(_ => _.IsPullRequest || (string.Equals(_.State, "CLOSED", StringComparison.Ordinal) && !string.Equals(_.StateReason, "COMPLETED", StringComparison.Ordinal))))
        {
            return new(row.Id, RowVerdictKind.Unowned, 0, 0, "Owner is a pull request or was closed without completion.");
        }

        if (string.Equals(row.Status, "pending-owner", StringComparison.Ordinal))
        {
            return owners.All(_ => string.Equals(_.State, "CLOSED", StringComparison.Ordinal))
                ? new(row.Id, RowVerdictKind.StalePending, 0, 0, "All owners completed without wiring the row.")
                : new(row.Id, RowVerdictKind.Pending, 0, 0, string.Join("; ", row.RequiredProofs));
        }

        return EvaluateProofs(row, jobs, tests);
    }

    static OwnerIssue FindOwner(int number, IReadOnlyList<OwnerIssue> issues)
    {
        var found = issues.Where(_ => _.Number == number).ToArray();
        if (found.Length != 1 || (!string.Equals(found[0].State, "OPEN", StringComparison.Ordinal) && !string.Equals(found[0].State, "CLOSED", StringComparison.Ordinal))
            || (!found[0].IsPullRequest && string.Equals(found[0].State, "CLOSED", StringComparison.Ordinal) && found[0].StateReason is null))
        {
            throw new CannotEvaluate($"Issue #{number} was not looked up successfully and uniquely.");
        }

        return found[0];
    }

    static RowVerdict EvaluateProofs(MatrixRow row, IReadOnlyDictionary<string, JobResult> jobs, IReadOnlyList<TestResult> tests)
    {
        var requiredJobs = row.Jobs.Select(name => ReadJob(name, jobs)).ToArray();
        if (requiredJobs.Any(_ => string.Equals(_.Result, "failure", StringComparison.Ordinal) || string.Equals(_.Result, "cancelled", StringComparison.Ordinal)))
        {
            return new(row.Id, RowVerdictKind.Red, 0, 0, "A required job failed or was cancelled.");
        }

        var selected = tests.Where(test => row.Specs.Any(spec => Matches(test.Name, spec.Selector)))
            .GroupBy(_ => _.Name, StringComparer.Ordinal).Select(_ => new TestResult(_.Key, _.All(test => test.Passed))).ToArray();
        var planted = row.PlantedDefects.Count(defect => tests.Any(_ => string.Equals(_.Name, defect.Spec, StringComparison.Ordinal)) && tests.Where(_ => string.Equals(_.Name, defect.Spec, StringComparison.Ordinal)).All(_ => _.Passed));
        if (row.Specs.Any(spec => selected.Count(test => Matches(test.Name, spec.Selector)) < spec.Minimum)
            || selected.Any(_ => !_.Passed) || planted != row.PlantedDefects.Length)
        {
            return new(row.Id, RowVerdictKind.Red, selected.Length, planted, "A selector is below its minimum, a selected test did not pass, or a planted-defect spec is absent or not passing.");
        }

        return requiredJobs.Any(_ => _.Skipped)
            ? new(row.Id, RowVerdictKind.Skipped, selected.Length, planted, "A required job skipped its check.")
            : new(row.Id, RowVerdictKind.Green, selected.Length, planted, "All declared proofs passed.");
    }

    static JobResult ReadJob(string name, IReadOnlyDictionary<string, JobResult> jobs)
    {
        if (!jobs.TryGetValue(name, out var job) || job is null || (!string.Equals(job.Result, "success", StringComparison.Ordinal) && !string.Equals(job.Result, "failure", StringComparison.Ordinal)
            && !string.Equals(job.Result, "cancelled", StringComparison.Ordinal) && !string.Equals(job.Result, "skipped", StringComparison.Ordinal)))
        {
            throw new CannotEvaluate($"No valid result for job '{name}'.");
        }

        return job;
    }

    static bool Matches(string name, string selector) => string.Equals(name, selector, StringComparison.Ordinal) || name.StartsWith($"{selector}.", StringComparison.Ordinal);
}

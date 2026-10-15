// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// The exception that is thrown when pull progress or ordering contradicts the event log.
/// </summary>
internal sealed class ProbePullCorrectnessFailure(string message) : Exception(message);

internal static partial class Probe
{
    internal static int ExitCode(string status) => status switch
    {
        "COMPLETE" or "COMPLETE_TIMING_ONLY" or "EXPECTED_DROP" => 0,
        "DEFECT" => 1,
        _ => 2
    };

    internal static void ApplyCorrectness(ProbeRun run)
    {
        // Attribution, snapshot or cleanup failures must never overwrite a proven correctness defect.
        if (run.PullCorrectnessFailure is not null) run.Status = "DEFECT";
        if (!run.RegisteredMs.HasValue || run.Expected == 0 || run.Accounting is not { } accounting) return;
        if (accounting.Duplicates == 0 && accounting.FirstDeliveryOrderViolations == 0 &&
            accounting.Unexpected.Length == 0 && accounting.Missing.Length == 0 && accounting.PayloadMismatches == 0) return;
        var expectedDrop = run.IsPull && run.DeliberateDrop && run.PullOptions?.DropSequence is { } drop &&
            run.Status is ("COMPLETE" or "COMPLETE_TIMING_ONLY") && run.PullTailExact == true && run.AppendFinished &&
            accounting.Missing.Length == 1 && accounting.Missing[0].SequenceNumber == drop &&
            accounting.Duplicates == 0 && accounting.FirstDeliveryOrderViolations == 0 &&
            accounting.Unexpected.Length == 0 && accounting.PayloadMismatches == 0;
        run.Status = expectedDrop ? "EXPECTED_DROP" : "DEFECT";
        run.Reason += $"\nDelivery defect (planted={run.DeliberateDrop}): missing={accounting.Missing.Length}, duplicates={accounting.Duplicates}, order={accounting.FirstDeliveryOrderViolations}, unexpected={accounting.Unexpected.Length}, payload={accounting.PayloadMismatches}.";
    }

    static async Task CaptureContainerCpu(ProbeMongo mongo, ProbeRun run, JsonElement start)
    {
        try
        {
            run.ContainerCpu = JsonSerializer.SerializeToElement(new { start, end = await mongo.CpuSnapshot().WaitAsync(TimeSpan.FromSeconds(5)) });
        }
        catch (Exception error)
        {
            run.ContainerCpu = null;
            run.Reason += $"\nContainer CPU snapshot failed: {error.Message}";
        }
    }
}

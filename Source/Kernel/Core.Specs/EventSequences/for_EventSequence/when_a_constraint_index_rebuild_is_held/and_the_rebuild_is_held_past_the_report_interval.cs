// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_the_rebuild_is_held_past_the_report_interval : given.a_held_constraint_index_rebuild
{
    async Task Establish()
    {
        _stepStatus = JobStepStatus.Running;
        await HoldRebuild();
    }

    async Task Because()
    {
        for (var elapsed = TimeSpan.Zero; elapsed < EventSequence.ConstraintIndexRebuildHeldReportInterval; elapsed += EventSequence.ConstraintIndexRebuildLivenessInterval)
        {
            await PassLivenessCheck();
        }
        await PassLivenessCheck();
    }

    [Fact]
    void should_report_the_live_step_and_held_constraint_once()
    {
        var entry = _heldLogger.Entries.Single(item => item.Level == LogLevel.Information).Message;
        entry.ShouldContain("Constraint rebuild");
        entry.ShouldContain("is still holding unique");
        entry.ShouldContain(_heldRunId.Value.ToString());
        entry.ShouldContain(_heldRunId.Attempt.ToString());
        entry.ShouldContain(_jobId.ToString());
        entry.ShouldContain("Running");
    }

    [Fact] void should_not_warn_about_a_running_step() => _heldLogger.Entries.Exists(entry => entry.Level == LogLevel.Warning).ShouldBeFalse();
}

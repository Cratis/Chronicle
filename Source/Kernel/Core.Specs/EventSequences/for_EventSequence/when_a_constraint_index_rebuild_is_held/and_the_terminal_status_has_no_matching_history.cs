// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_the_terminal_status_has_no_matching_history : given.a_held_constraint_index_rebuild
{
    async Task Establish()
    {
        await HoldRebuild();
        _steps[0].StatusChanges[0].Status = JobStepStatus.Running;
    }

    async Task Because()
    {
        _clock.Advance(EventSequence.ConstraintIndexRebuildHeldReportInterval);
        await Tick();
        _result = await AppendValue("current");
    }

    [Fact] void should_refuse_governed_appends() => _result.Errors.ShouldContainOnly(AppendError.ConstraintIndexUnavailable);
    [Fact] async Task should_not_fold_history() => (await Values("source")).ShouldBeEmpty();
    [Fact] void should_report_undetermined_liveness() => _heldLogger.Entries.Exists(entry => entry.Level == LogLevel.Warning && entry.Message.Contains("missing or inconsistent status history; restart the Kernel to recover")).ShouldBeTrue();
}

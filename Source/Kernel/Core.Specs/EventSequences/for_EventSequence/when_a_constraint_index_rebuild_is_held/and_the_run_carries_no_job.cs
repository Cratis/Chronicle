// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_the_run_carries_no_job : given.a_held_constraint_index_rebuild
{
    async Task Establish()
    {
        _heldRunId = new(_heldRunId.Value);
        await HoldRebuild();
    }

    async Task Because()
    {
        _clock.Advance(EventSequence.ConstraintIndexRebuildHeldReportInterval);
        await Tick();
        _result = await AppendValue("current");
    }

    [Fact] void should_warn_that_restart_is_needed() => _heldLogger.Entries.Exists(entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning && entry.Message.Contains("no job; restart the Kernel to recover")).ShouldBeTrue();
    [Fact] void should_not_query_job_storage() => _jobSteps.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_not_fold_history() => (await Values("source")).ShouldBeEmpty();
    [Fact] void should_remain_fail_closed() => _result.Errors.ShouldContainOnly(AppendError.ConstraintIndexUnavailable);
}

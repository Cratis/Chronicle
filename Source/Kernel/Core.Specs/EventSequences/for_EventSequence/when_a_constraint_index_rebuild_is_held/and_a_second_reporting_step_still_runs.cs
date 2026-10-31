// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_a_second_reporting_step_still_runs : given.a_held_constraint_index_rebuild
{
    ConstraintIndexRebuildRunId _second;

    async Task Establish()
    {
        await HoldRebuild();
        _second = new(Guid.NewGuid(), Guid.NewGuid(), _clock.GetUtcNow(), _jobId);
        _steps.Add(new() { Id = new(_jobId, _second.Value), Status = JobStepStatus.Running });
        await _eventSequence.ConstraintIndexRebuildStarting(_second, [_uniqueDefinition.Name]);
    }

    async Task Because()
    {
        await PassLivenessCheck();
        _result = await AppendValue("current");
    }

    [Fact] async Task should_not_fold_while_another_reporter_can_write() => (await Values("source")).ShouldBeEmpty();
    [Fact] void should_keep_the_refusal() => _result.Errors.ShouldContainOnly(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_hand_over_only_the_failed_reporter() => _heldLogger.Entries.Count(entry => entry.Message.Contains("Releasing constraint rebuild")).ShouldEqual(1);
}

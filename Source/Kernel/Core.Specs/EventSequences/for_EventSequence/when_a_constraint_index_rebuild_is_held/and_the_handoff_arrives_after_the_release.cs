// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_the_handoff_arrives_after_the_release : given.a_held_constraint_index_rebuild
{
    async Task Establish()
    {
        await HoldRebuild();
        var second = new ConstraintIndexRebuildRunId(Guid.NewGuid(), Guid.NewGuid(), _clock.GetUtcNow(), _jobId);
        _steps.Add(new() { Id = new(_jobId, second.Value), Status = JobStepStatus.Running });
        await _eventSequence.ConstraintIndexRebuildStarting(second, [_uniqueDefinition.Name]);
    }

    async Task Because()
    {
        await PassLivenessCheck();
        await _eventSequence.ConstraintIndexRebuilt(_heldRunId, [_uniqueDefinition.Name], new(true, 1));
        await CompleteCatchUp();
        _result = await AppendValue("current");
    }

    [Fact] void should_keep_the_second_reporters_mark() => _result.Errors.ShouldContainOnly(AppendError.ConstraintIndexUnavailable);
    [Fact] async Task should_not_repeat_claim_writes() => (await Values("source")).ShouldBeEmpty();
}

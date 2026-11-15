// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_the_step_status_cannot_be_read : given.a_held_constraint_index_rebuild
{
    async Task Establish()
    {
        await HoldRebuild();
        _jobSteps.GetForJob(_jobId, Arg.Any<JobStepStatus[]>()).Returns(_ => Catch<IImmutableList<JobStepState>>.Failed(new given.SimulatedStateWriteError()));
    }

    async Task Because()
    {
        await PassLivenessCheck();
        await Tick();
        _result = await AppendValue("current");
    }

    [Fact] async Task should_not_fold_history() => (await Values("source")).ShouldBeEmpty();
    [Fact] void should_remain_fail_closed() => _result.Errors.ShouldContainOnly(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_warn_about_unreadable_storage() => _heldLogger.Entries.Exists(entry => entry.Message.StartsWith("Could not read the job step of constraint rebuild", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] async Task should_throttle_failed_lookups() => await _jobSteps.Received(1).GetForJob(_jobId, Arg.Any<JobStepStatus[]>());
}

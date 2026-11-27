// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_fast_forwarding;

public class and_an_accepted_repair_is_waiting_for_its_continuation : given.an_observer_accepting_a_catch_up_handover
{
    FastForwardResult _result;
    Func<CancellationToken, Task> _staleContinuation;

    async Task Establish()
    {
        await _observer.CaughtUp(JobId.NotSet, 12UL, 12UL);
        _staleContinuation = _silo.TimerRegistry.Mock.Invocations
            .Last(_ => _.Method.Name == "RegisterGrainTimer" &&
                _.Arguments.OfType<GrainTimerCreationOptions>().Any(options => options.DueTime == TimeSpan.Zero))
            .Arguments.OfType<Func<CancellationToken, Task>>().Single();
        _stateStorage.State.CatchingUpPartitions.Clear();
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)12UL);
    }

    async Task Because()
    {
        _result = await _observer.FastForward();
        await _staleContinuation(CancellationToken.None);
        await _observer.HandoverRepairCaughtUp("B", 10UL, 13UL, EventSequenceNumber.Unavailable, succeeded: false);
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_not_treat_pending_repairs_as_already_at_tail() => _result.Outcome.ShouldEqual(FastForwardOutcome.FastForwarded);
    [Fact] void should_clear_the_durable_repair() => _writes.PersistedState.PendingCatchUpRepairs!.ShouldBeEmpty();
    [Fact] void should_leave_no_partition_hold() => _stateStorage.State.CatchingUpPartitions.ShouldBeEmpty();
    [Fact] void should_not_restart_the_discarded_repair() => _jobsManager.DidNotReceive()
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>());
    [Fact] void should_ignore_a_late_failure_from_the_discarded_repair() => _failedPartitionsStorage.State.Partitions.ShouldBeEmpty();
    [Fact] void should_stay_at_the_fast_forwarded_position() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)13UL);
}

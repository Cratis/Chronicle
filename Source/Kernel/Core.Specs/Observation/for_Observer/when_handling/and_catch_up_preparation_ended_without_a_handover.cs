// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// No job took ownership of the catch-up, so preparation ends without a round handing over. The event it held back is no longer owned, and another partition crossing it is reported once.
/// </summary>
public class and_catch_up_preparation_ended_without_a_handover : given.an_observer_with_skip_detection
{
    readonly TaskCompletionSource<IImmutableList<JobState>> _heldLookup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Task _catchUp;
    int _reportsBeforeCrossing;

    async Task Establish()
    {
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_ => _heldLookup.Task);
        _jobsManager.Resume(Arg.Any<JobId>()).Returns(false);
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)103UL);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.NotFound);
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 102UL };
        await _observer.PartitionFailed("P", 100UL, ["Failed handler"], string.Empty);
        _subscriber.ClearReceivedCalls();
    }

    async Task Because()
    {
        _catchUp = _observer.CatchUp();
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 102UL)]);

        // A stopped job refuses to resume, which lowers the preparing flag without any handover.
        _heldLookup.SetResult(ImmutableList.Create(new JobState
        {
            Id = JobId.New(),
            Status = JobStatus.Stopped,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
        }));
        await _catchUp;
        _reportsBeforeCrossing = _suspectedSkips;
        await _observer.FailedPartitionRecovered("P", 103UL);
    }

    [Fact] void should_not_report_before_the_position_crosses_the_event() => _reportsBeforeCrossing.ShouldEqual(0);
    [Fact] void should_report_exactly_one_suspected_skip() => _metrics.For(EventsSuspectedSkipped).Select(_ => _.Value).SequenceEqual([1]).ShouldBeTrue();
}

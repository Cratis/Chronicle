// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// A projection is caught up in one sweep as well, so an event for a partition appended after the sweep enumerated
/// its partitions is left to the sweep rather than applied live and then again by the sweep (#5786).
/// </summary>
public class and_a_projection_catch_up_did_not_register_the_partition : given.an_observer_with_subscription_for_specific_event_type
{
    async Task Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection };
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 10UL };
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(ObserverSubscriberResult.Ok(12UL));
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>()).Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
        await _observer.CatchUp();
        await _observer.RegisterCatchingUpPartitions(["registered"]);
        _subscriber.ClearReceivedCalls();
    }

    async Task Because() => await _observer.Handle("appended-after-enumeration", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 12UL)]);

    [Fact] void should_not_apply_the_event_live() => _subscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_leave_the_position_for_the_sweep() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)10UL);
}

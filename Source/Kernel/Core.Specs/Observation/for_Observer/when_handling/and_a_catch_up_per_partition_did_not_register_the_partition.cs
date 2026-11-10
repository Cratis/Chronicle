// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// A catch-up per partition only reads the partitions it registered, so a partition it did not register is handled live.
/// </summary>
public class and_a_catch_up_per_partition_did_not_register_the_partition : given.an_observer_with_subscription_for_specific_event_type
{
    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 10UL };
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(ObserverSubscriberResult.Ok(12UL));
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>()).Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
        await _observer.CatchUp();
        await _observer.RegisterCatchingUpPartitions(["registered"]);
        _subscriber.ClearReceivedCalls();
    }

    async Task Because() => await _observer.Handle("not-registered", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 12UL)]);

    [Fact] void should_handle_the_event_live() => _subscriber.Received(1).OnNext((Key)"not-registered", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] async Task should_stop_holding_back_live_delivery() => (await _observer.IsPreparingCatchup()).ShouldBeFalse();
}

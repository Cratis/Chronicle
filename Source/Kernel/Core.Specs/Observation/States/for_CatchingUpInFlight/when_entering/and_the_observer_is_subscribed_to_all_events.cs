// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.when_entering;

public class and_the_observer_is_subscribed_to_all_events : given.a_catching_up_in_flight_state
{
    readonly Key _partition = "shared-partition";
    CatchUpObserverPartitionRequest _request;

    void Establish()
    {
        _storedState = _storedState with { SubscribesToAllEvents = true, LastHandledEventSequenceNumber = (EventSequenceNumber)4UL, InFlightPartitions = new HashSet<Key> { _partition } };
        _jobsManager
            .When(_ => _.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>()))
            .Do(callInfo => _request = callInfo.Arg<CatchUpObserverPartitionRequest>());
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_start_the_job_with_a_wildcard_scope() => _request.ReaderScope!.IsWildcard.ShouldBeTrue();
}

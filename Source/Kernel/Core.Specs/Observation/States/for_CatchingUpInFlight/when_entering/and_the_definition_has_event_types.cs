// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.when_entering;

public class and_the_definition_has_event_types : given.a_catching_up_in_flight_state
{
    static readonly EventType _eventType = new("11111111-1111-1111-1111-111111111111", EventTypeGeneration.First);
    readonly Key _partition = "shared-partition";
    CatchUpObserverPartitionRequest _request;

    void Establish()
    {
        _definitionState.State = ObserverDefinition.Empty with { EventTypes = [_eventType] };
        _storedState = _storedState with { LastHandledEventSequenceNumber = (EventSequenceNumber)4UL, InFlightPartitions = new HashSet<Key> { _partition } };
        _jobsManager
            .When(_ => _.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>()))
            .Do(callInfo => _request = callInfo.Arg<CatchUpObserverPartitionRequest>());
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_start_the_job_with_the_event_types_as_an_explicit_scope() => _request.ReaderScope!.EventTypes.ShouldContainOnly([_eventType]);
}

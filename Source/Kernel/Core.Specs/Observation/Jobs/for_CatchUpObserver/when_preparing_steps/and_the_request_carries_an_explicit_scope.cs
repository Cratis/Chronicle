// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.when_preparing_steps;

public class and_the_request_carries_an_explicit_scope : given.a_catch_up_observer_job
{
    static readonly EventType _eventType = new("11111111-1111-1111-1111-111111111111", EventTypeGeneration.First);
    static readonly Key _key = (Key)"partition-1";
    ObserverReaderScope _scope;

    void Establish()
    {
        _scope = ObserverReaderScope.Explicit([_eventType]);
        _request = _request with { ObserverType = ObserverType.Reactor, ReaderScope = _scope };
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys(_key));
    }

    async Task Because() => await StartAndLetTheStepsCome();

    [Fact] void should_look_the_key_index_up_with_the_scope() => _keyIndexes.Received(1).GetFor(_request.ObserverKey, _scope);
    [Fact] void should_hand_the_scope_to_the_step() => ((HandleEventsForPartitionArguments)_job.PreparedSteps![0].Request).ReaderScope.ShouldBeSame(_scope);
}

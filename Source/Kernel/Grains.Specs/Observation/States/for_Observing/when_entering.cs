// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Grains.EventSequences;
using Orleans.Streams;

namespace Cratis.Chronicle.Grains.Observation.States.for_Observing;

public class when_entering : given.an_observing_state
{
    void Establish() => _storedState = _storedState with { NextEventSequenceNumber = 42UL };

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_subscribe_to_stream() => _appendedEventsQueues.Received(1).Subscribe(_observerKey, _eventTypes);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Grains.EventSequences;
using Orleans.Streams;

namespace Cratis.Chronicle.Grains.Observation.States.for_Observing;

public class when_entering : given.an_observing_state
{
    void Establish() => stored_state = stored_state with { NextEventSequenceNumber = 42UL };

    async Task Because() => resulting_stored_state = await state.OnEnter(stored_state);

    [Fact] void should_subscribe_to_stream() => appended_events_queues.Verify(_ => _.Subscribe(observer_key, event_types), Once());
}

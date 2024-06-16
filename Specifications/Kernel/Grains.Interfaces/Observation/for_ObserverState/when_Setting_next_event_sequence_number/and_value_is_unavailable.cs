// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Grains.Observation.for_ObserverState.when_Setting_next_event_sequence_number;

public class and_value_is_unavailable : Specification
{
    ObserverState state;

    void Establish()
    {
        state = new ObserverState
        {
            NextEventSequenceNumber = 42
        };
    }

    void Because() => state = state with { NextEventSequenceNumber = EventSequenceNumber.Unavailable };

    [Fact] void should_set_next_event_sequence_number_to_first() => state.NextEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);
}

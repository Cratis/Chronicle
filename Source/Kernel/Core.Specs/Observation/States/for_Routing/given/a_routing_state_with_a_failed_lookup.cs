// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.States.for_Routing.given;

public class a_routing_state_with_a_failed_lookup : a_routing_state
{
    void Establish()
    {
        _storedState = _storedState with { NextEventSequenceNumber = 5UL };
        _tailEventSequenceNumbers = _tailEventSequenceNumbers with { Tail = 20UL };
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.StorageError);
    }
}

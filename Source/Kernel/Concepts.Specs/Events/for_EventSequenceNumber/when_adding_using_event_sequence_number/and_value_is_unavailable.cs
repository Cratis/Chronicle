// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_EventSequenceNumber.when_adding_using_event_sequence_number;

public class and_value_is_unavailable : Specification
{
    EventSequenceNumber _result;

    void Because() => _result = EventSequenceNumber.Unavailable + (EventSequenceNumber)42UL;

    [Fact] void should_remain_unavailable() => _result.ShouldEqual(EventSequenceNumber.Unavailable);
}

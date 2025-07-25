// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_EventSequenceNumber.when_adding_using_event_sequence_number;

public class and_value_is_random : Specification
{
    static Random _random = new();
    EventSequenceNumber _result;
    EventSequenceNumber _value;

    void Establish() => _value = (ulong)_random.Next(0, 100);

    void Because() => _result = _value + (EventSequenceNumber)42UL;

    [Fact] void should_add_value() => _result.ShouldEqual((EventSequenceNumber)(_value.Value + 42));
}

// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.for_EventSequenceNumber.when_subtracting_using_event_sequence_number;

public class and_value_is_max : Specification
{
    EventSequenceNumber result;

    void Because() => result = EventSequenceNumber.Max - (EventSequenceNumber)42UL;

    [Fact] void should_remain_max() => result.ShouldEqual(EventSequenceNumber.Max);
}

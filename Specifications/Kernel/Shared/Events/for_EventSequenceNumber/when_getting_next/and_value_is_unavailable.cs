// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.for_EventSequenceNumber.when_getting_next;

public class and_value_is_unavailable : Specification
{
    EventSequenceNumber result;

    void Because() => result = EventSequenceNumber.Unavailable.Next();

    [Fact] void should_remain_unavailable() => result.ShouldEqual(EventSequenceNumber.Unavailable);
}

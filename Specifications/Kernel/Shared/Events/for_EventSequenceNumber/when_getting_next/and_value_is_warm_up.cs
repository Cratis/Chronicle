// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventSequenceNumber.when_getting_next;

public class and_value_is_warm_up : Specification
{
    EventSequenceNumber result;

    void Because() => result = EventSequenceNumber.WarmUp.Next();

    [Fact] void should_remain_warm_up() => result.ShouldEqual(EventSequenceNumber.WarmUp);
}

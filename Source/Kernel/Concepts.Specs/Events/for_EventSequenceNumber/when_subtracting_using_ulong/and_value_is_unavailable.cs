// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_EventSequenceNumber.when_subtracting_using_ulong;

public class and_value_is_unavailable : Specification
{
    EventSequenceNumber result;

    void Because() => result = EventSequenceNumber.Unavailable - 42UL;

    [Fact] void should_remain_unavailable() => result.ShouldEqual(EventSequenceNumber.Unavailable);
}

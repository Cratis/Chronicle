// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_EventSequenceNumber.when_getting_next;

public class and_value_is_unavailable : Specification
{
    EventSequenceNumber _result;

    void Because() => _result = EventSequenceNumber.Unavailable.Next();

    [Fact] void should_remain_unavailable() => _result.ShouldEqual(EventSequenceNumber.Unavailable);
}

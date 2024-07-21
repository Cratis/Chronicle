// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_checking_if_type_is_event_type;

public class and_type_has_event_type_attribute : Specification
{
    [EventType("01547ac7-25f6-439b-bc20-2422479ef29a")]
    class MyEvent;

    bool result;

    void Because() => result = typeof(MyEvent).IsEventType([]);

    [Fact] void should_be_considered_an_event_type() => result.ShouldBeTrue();
}

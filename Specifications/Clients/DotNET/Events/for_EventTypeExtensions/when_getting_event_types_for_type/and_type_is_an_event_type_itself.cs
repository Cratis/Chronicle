// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.for_EventTypeExtensions.when_getting_event_types_for_type;

public class and_type_is_an_event_type_itself : Specification
{
    [EventType("01547ac7-25f6-439b-bc20-2422479ef29a")]
    class MyEvent
    {
    }

    IEnumerable<Type> result;

    void Because() => result = typeof(MyEvent).GetEventTypes(new[] { typeof(MyEvent) });

    [Fact] void should_return_the_type() => result.ShouldContainOnly(typeof(MyEvent));
}

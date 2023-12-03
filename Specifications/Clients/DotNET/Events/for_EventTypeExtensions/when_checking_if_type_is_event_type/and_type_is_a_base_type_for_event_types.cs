// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.for_EventTypeExtensions.when_checking_if_type_is_event_type;

public class and_type_is_a_base_type_for_event_types : Specification
{
    class MyEvent
    {
    }

    class MyDerivedEvent : MyEvent
    {
    }

    bool result;

    void Because() => result = typeof(MyEvent).IsEventType(new[] { typeof(MyDerivedEvent) });

    [Fact] void should_be_considered_an_event_type() => result.ShouldBeTrue();
}

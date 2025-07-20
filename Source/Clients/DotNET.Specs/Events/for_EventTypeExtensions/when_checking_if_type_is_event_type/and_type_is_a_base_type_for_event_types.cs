// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_checking_if_type_is_event_type;

public class and_type_is_a_base_type_for_event_types : Specification
{
    class MyEvent;

    class MyDerivedEvent : MyEvent;

    bool _result;

    void Because() => _result = typeof(MyEvent).IsEventType([typeof(MyDerivedEvent)]);

    [Fact] void should_be_considered_an_event_type() => _result.ShouldBeTrue();
}

// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.for_EventTypeExtensions.when_checking_if_type_is_event_type;

public class and_type_does_not_have_an_event_type_attribute_nor_is_it_a_base_type_for_event_types : Specification
{
    class MyEvent
    {
    }

    bool result;

    void Because() => result = typeof(MyEvent).IsEventType(Enumerable.Empty<Type>());

    [Fact] void should_not_be_considered_an_event_type() => result.ShouldBeFalse();
}

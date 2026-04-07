// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_getting_event_store_name;

public class and_type_has_no_event_store_attribute : Specification
{
    [EventType]
    class EventWithNoEventStoreAttribute;

    string? _result;

    void Because() => _result = typeof(EventWithNoEventStoreAttribute).GetEventStoreName();

    [Fact] void should_return_null() => _result.ShouldBeNull();
}

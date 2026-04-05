// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_getting_event_store_name;

public class and_type_has_class_level_event_store_attribute : Specification
{
    const string SourceEventStore = "some-event-store";

    [EventType]
    [EventStore(SourceEventStore)]
    class EventWithClassLevelEventStoreAttribute;

    string? _result;

    void Because() => _result = typeof(EventWithClassLevelEventStoreAttribute).GetEventStoreName();

    [Fact] void should_return_the_class_level_event_store_name() => _result.ShouldEqual(SourceEventStore);
}

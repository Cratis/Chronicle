// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter.when_reading_an_event_with_multiple_generations;

public class and_appended_generation_is_missing : Specification
{
    EventEntry _entry;
    EventType _result;

    void Establish() => _entry = new EventEntry
    {
        Type = new EventTypeId(Guid.NewGuid().ToString()),
        Content = "{\"1\":{},\"2\":{}}"
    };

    void Because() => _result = EventEntryConverter.GetEventType(_entry);

    [Fact] void should_keep_the_previous_highest_generation_behavior() => _result.Generation.ShouldEqual((EventTypeGeneration)2);
}

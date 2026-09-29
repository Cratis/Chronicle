// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.for_EventTypesStorageExtensions.when_getting_the_stored_schema;

public class and_the_event_type_is_not_stored : given.an_event_type_stored_at_two_generations
{
    JsonSchema? _schema;

    void Establish() => _eventTypes.GetFor(Arg.Any<IEnumerable<EventType>>())
        .Returns(Task.FromResult<IEnumerable<EventTypeSchema>>([]));

    async Task Because() => _schema = await _eventTypes.GetStoredSchemaFor(first_generation);

    [Fact] void should_return_no_schema() => _schema.ShouldBeNull();
}

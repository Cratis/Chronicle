// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage.when_resolving_schema_for_a_generation;

public class and_the_generation_is_unknown : given.an_event_types_storage
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(() => _storage.GetFor(_eventTypeId, new EventTypeGeneration(3)));

    [Fact] void should_not_substitute_another_generations_schema() => _exception.ShouldBeOfExactType<MissingEventSchemaForEventType>();
}

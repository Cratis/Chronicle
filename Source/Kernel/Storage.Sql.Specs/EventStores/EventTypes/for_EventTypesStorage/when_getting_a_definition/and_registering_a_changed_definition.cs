// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage.when_getting_a_definition;

public class and_registering_a_changed_definition : given.an_event_types_storage
{
    EventTypeDefinition _first;
    EventTypeDefinition _updated;

    async Task Because()
    {
        _first = await _storage.GetDefinition(_eventTypeId);

        await _storage.Register(_first with
        {
            Generations = [.. _first.Generations, new EventTypeGenerationDefinition(new EventTypeGeneration(3), await Cratis.Chronicle.Schemas.JsonSchema.FromJsonAsync(SchemaFor("thirdGenerationValue")))]
        });
        _updated = await _storage.GetDefinition(_eventTypeId);
    }

    [Fact] void should_return_the_new_generation_after_registration() => _updated.Generations.Select(_ => _.Generation).ShouldContain(new EventTypeGeneration(3));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage.when_getting_a_definition;

public class and_registering_a_changed_definition : given.an_event_types_storage
{
    EventTypeDefinition _first;
    EventTypeDefinition _cached;
    EventTypeDefinition _updated;
    EventTypeDefinition _recached;
    int _readsBeforeRegistration;
    int _readsAfterRegistration;

    async Task Because()
    {
        _first = await _storage.GetDefinition(_eventTypeId);
        _cached = await _storage.GetDefinition(_eventTypeId);
        _readsBeforeRegistration = _database.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(IDatabase.EventStore));

        await _storage.Register(_first with
        {
            Generations = [.. _first.Generations, new EventTypeGenerationDefinition(new EventTypeGeneration(3), await Cratis.Chronicle.Schemas.JsonSchema.FromJsonAsync(SchemaFor("thirdGenerationValue")))]
        });
        _updated = await _storage.GetDefinition(_eventTypeId);
        _recached = await _storage.GetDefinition(_eventTypeId);
        _readsAfterRegistration = _database.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(IDatabase.EventStore));
    }

    [Fact] void should_read_the_original_definition_only_once() => _readsBeforeRegistration.ShouldEqual(1);
    [Fact] void should_return_the_new_generation_after_registration() => _updated.Generations.Select(_ => _.Generation).ShouldContain(new EventTypeGeneration(3));
    [Fact] void should_read_the_updated_definition_only_once() => _readsAfterRegistration.ShouldEqual(_readsBeforeRegistration + 2);
    [Fact] void should_reuse_the_updated_definition() => _recached.ShouldBeSame(_updated);
}

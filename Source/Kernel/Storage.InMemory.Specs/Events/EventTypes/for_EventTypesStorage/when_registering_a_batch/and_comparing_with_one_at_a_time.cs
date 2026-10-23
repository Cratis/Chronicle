// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes.for_EventTypesStorage.when_registering_a_batch;

public class and_comparing_with_one_at_a_time : given.a_batch_of_event_types
{
    EventTypesStorage _oneAtATime;
    IEnumerable<EventTypeSchema> _batched;
    IEnumerable<EventTypeSchema> _single;
    IEnumerable<EventTypeDefinition> _batchedDefinitions;
    IEnumerable<EventTypeDefinition> _singleDefinitions;

    void Establish() => _oneAtATime = new();

    async Task Because()
    {
        await _storage.Register(_batch);
        foreach (var item in _batch)
        {
            if (item.Definition.Generations.Count() == 1 && !item.Definition.Migrations.Any())
            {
                var generation = item.Definition.Generations.Single();
                await _oneAtATime.Register(new EventType(item.Definition.Id, generation.Generation, item.Definition.Tombstone), generation.Schema, item.Definition.Owner, item.Source, item.Visibility, item.Origin);
            }
            else
            {
                await _oneAtATime.Register(item.Definition);
            }
        }

        _batched = await _storage.GetLatestForAllEventTypes();
        _single = await _oneAtATime.GetLatestForAllEventTypes();
        _batchedDefinitions = await _storage.GetAllDefinitions();
        _singleDefinitions = await _oneAtATime.GetAllDefinitions();
    }

    [Fact] void should_have_the_same_latest_schemas() => Describe(_batched).ShouldEqual(Describe(_single));
    [Fact] void should_have_the_same_definitions() => Describe(_batchedDefinitions).ShouldEqual(Describe(_singleDefinitions));

    static string Describe(IEnumerable<EventTypeSchema> schemas) => string.Join(
        '|',
        schemas.OrderBy(_ => _.Type.Id.Value).Select(_ => $"{_.Type.Id.Value}/{_.Type.Generation.Value}/{_.Owner}/{_.Source}/{_.Visibility}/{_.Origin}/{_.Schema.ToJson()}"));

    static string Describe(IEnumerable<EventTypeDefinition> definitions) => string.Join(
        '|',
        definitions.OrderBy(_ => _.Id.Value).Select(_ => $"{_.Id.Value}/{_.Owner}/{string.Join(',', _.Generations.Select(generation => $"{generation.Generation.Value}:{generation.Schema.ToJson()}"))}/{string.Join(',', _.Migrations.Select(migration => $"{migration.FromGeneration.Value}-{migration.ToGeneration.Value}"))}"));
}

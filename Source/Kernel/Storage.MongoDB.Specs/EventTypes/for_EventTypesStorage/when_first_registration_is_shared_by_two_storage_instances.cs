// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage;

[Collection(MongoDBCollection.Name)]
public class when_first_registration_is_shared_by_two_storage_instances(MongoDBFixture fixture) : given.two_event_types_storages(fixture)
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    async Task should_register_one_schema_across_independent_storage_instances(bool identical, bool existingType)
    {
        var generation = new EventTypeGeneration(existingType ? 2U : 1U);
        if (existingType) await _storageA.RegisterIfAbsent(Registration(1, "previous"));
        var first = Registration(generation, "first");
        var second = Registration(generation, identical ? "first" : "second");
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = new[] { Register(_storageA, first), Register(_storageB, second) };
        start.SetResult();
        var results = await Task.WhenAll(tasks);
        var definition = await _storageA.GetDefinition(first.Definition.Id);

        results.Count(_ => _.Changed).ShouldEqual(1);
        results.Sum(_ => _.AddedGenerations.Count).ShouldEqual(1);
        results.Count(_ => _.Conflict is not null).ShouldEqual(identical ? 0 : 1);
        definition.Generations.Count().ShouldEqual(existingType ? 2 : 1);
        var winner = (results[0].Changed ? first : second).Definition.Generations.Single().Schema.Properties.Keys.Single();
        definition.Generations.Single(_ => _.Generation == generation).Schema.Properties.Keys.ShouldContain(winner);

        async Task<EventTypeRegistrationResult> Register(IEventTypesStorage storage, EventTypeToRegister registration)
        {
            await start.Task;
            return await storage.RegisterIfAbsent(registration);
        }
    }

    static EventTypeToRegister Registration(EventTypeGeneration generation, string property)
    {
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"placeholder":{"type":"string"}}}""".Replace("placeholder", property, StringComparison.Ordinal));
        return new(new EventTypeDefinition("synthetic-registration", EventTypeOwner.Client, false, [new EventTypeGenerationDefinition(generation, schema)], []), EventTypeSource.Code);
    }
}

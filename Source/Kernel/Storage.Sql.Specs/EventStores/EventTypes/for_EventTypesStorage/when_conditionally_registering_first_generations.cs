// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_conditionally_registering_first_generations : given.an_event_types_storage
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    async Task should_preserve_the_winner_when_the_conditional_write_loses(bool identical, bool existingType)
    {
        var id = existingType ? _eventTypeId : new EventTypeId("synthetic-first-registration");
        var generation = existingType ? new EventTypeGeneration(3) : EventTypeGeneration.First;
        var winner = Registration(id, generation, "winner");
        var loser = Registration(id, generation, identical ? "winner" : "loser");
        var interceptor = new given.BeforeRegistrationWrite(async () => await _storage.RegisterIfAbsent(winner));
        var competingStorage = WithInterceptor(interceptor);

        var result = await competingStorage.RegisterIfAbsent(loser);
        var stored = await new EventTypesStorage(EventStoreName.NotSet, _database).GetDefinition(id);

        result.Changed.ShouldBeFalse();
        result.AddedGenerations.ShouldBeEmpty();
        if (identical) result.Conflict.ShouldBeNull();
        else result.Conflict.ShouldEqual(generation);
        stored.Generations.Single(_ => _.Generation == generation).Schema.Properties.Keys.ShouldContain("winner");
        stored.Generations.Count().ShouldEqual(existingType ? 3 : 1);
        interceptor.Writes.ShouldEqual(1);
    }

    [Fact]
    async Task should_reject_a_different_schema_committed_before_the_storage_read()
    {
        var winner = Registration("synthetic-before-read", EventTypeGeneration.First, "winner");
        await _storage.RegisterIfAbsent(winner);
        var result = await _storage.RegisterIfAbsent(Registration(winner.Definition.Id, EventTypeGeneration.First, "loser"));
        result.Conflict.ShouldEqual(EventTypeGeneration.First);
        (await _storage.GetDefinition(winner.Definition.Id)).Generations.Single().Schema.Properties.Keys.ShouldContain("winner");
    }

    [Fact]
    async Task should_retry_without_losing_a_concurrently_added_different_generation()
    {
        var interceptor = new given.BeforeRegistrationWrite(async () =>
            await _storage.RegisterIfAbsent(Registration(_eventTypeId, new(4), "other")));
        var result = await WithInterceptor(interceptor).RegisterIfAbsent(Registration(_eventTypeId, new(3), "incoming"));
        var stored = await new EventTypesStorage(EventStoreName.NotSet, _database).GetDefinition(_eventTypeId);
        result.Conflict.ShouldBeNull();
        result.AddedGenerations.ShouldContain(new EventTypeGeneration(3));
        stored.Generations.Count().ShouldEqual(4);
        stored.Generations.Single(_ => _.Generation == new EventTypeGeneration(4)).Schema.Properties.Keys.ShouldContain("other");
    }

    static EventTypeToRegister Registration(EventTypeId id, EventTypeGeneration generation, string property) =>
        new(new EventTypeDefinition(id, EventTypeOwner.Client, false, [new EventTypeGenerationDefinition(generation, JsonSchema.FromJson(SchemaFor(property)))], []), EventTypeSource.Code);

    EventTypesStorage WithInterceptor(given.BeforeRegistrationWrite interceptor)
    {
        var database = Substitute.For<IDatabase>();
        database.EventStore(Arg.Any<EventStoreName>()).Returns(_ =>
        {
            var options = new DbContextOptionsBuilder<EventStoreDbContext>()
                .UseSqlite(_connection).AddConceptAsSupport().WithoutServiceProviderCaching()
                .AddInterceptors(interceptor).Options;
            return Task.FromResult(new DbContextScope<EventStoreDbContext>(new EventStoreDbContext(options), () => { }));
        });
        return new(EventStoreName.NotSet, database);
    }
}

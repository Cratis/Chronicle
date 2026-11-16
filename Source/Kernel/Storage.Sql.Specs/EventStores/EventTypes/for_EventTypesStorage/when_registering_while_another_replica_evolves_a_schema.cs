// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_registering_while_another_replica_evolves_a_schema : given.an_event_types_storage
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_retry_registration_without_overwriting_the_evolved_generation(bool fullDefinition)
    {
        var original = await JsonSchema.FromJsonAsync(SchemaFor(FirstGenerationProperty));
        var evolved = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"firstGenerationValue":{"type":"string"},"optional":{"type":"string"}}}
            """);
        var replacementSucceeded = false;
        var interceptor = new given.BeforeRegistrationWrite(async () =>
            replacementSucceeded = await _storage.TryReplaceSchema(_eventTypeId, _firstGeneration, original, evolved));
        var registrationStorage = CreateRegistrationStorage(interceptor);
        var newGeneration = new EventTypeGeneration(3);
        var newSchema = await JsonSchema.FromJsonAsync(SchemaFor("thirdGeneration"));

        var changed = fullDefinition
            ? await registrationStorage.Register(new EventTypeDefinition(
                _eventTypeId,
                EventTypeOwner.Client,
                false,
                [new(_firstGeneration, original), new(newGeneration, newSchema)],
                []))
            : await registrationStorage.Register(new Concepts.Events.EventType(_eventTypeId, newGeneration), newSchema);

        var cold = await new EventTypesStorage(EventStoreName.NotSet, _database).GetDefinition(_eventTypeId);
        replacementSucceeded.ShouldBeTrue();
        changed.ShouldBeTrue();
        interceptor.Writes.ShouldEqual(2);
        cold.Generations.Single(_ => _.Generation == _firstGeneration).Schema.ToJson().ShouldEqual(evolved.ToJson());
        cold.Generations.ShouldContain(_ => _.Generation == newGeneration);
    }

    EventTypesStorage CreateRegistrationStorage(given.BeforeRegistrationWrite interceptor)
    {
        var database = Substitute.For<IDatabase>();
        database.EventStore(Arg.Any<EventStoreName>()).Returns(_ =>
        {
            var options = new DbContextOptionsBuilder<EventStoreDbContext>()
                .UseSqlite(_connection).AddConceptAsSupport().WithoutServiceProviderCaching()
                .AddInterceptors(interceptor).Options;
            var scope = new DbContextScope<EventStoreDbContext>(new EventStoreDbContext(options), () => { });
            return Task.FromResult(scope);
        });
        return new(EventStoreName.NotSet, database);
    }
}

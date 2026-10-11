// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Serialization;
using KernelEvents = KernelConcepts::Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Seeds discovered generations and migrations without running the system-sequence registrar.
/// </summary>
internal static class TestingEventTypes
{
    /// <summary>
    /// Seeds every discovered generation and its immutable migration version.
    /// </summary>
    /// <param name="storage">The event type storage.</param>
    /// <param name="eventTypes">The discovered types.</param>
    /// <param name="schemas">The schema generator.</param>
    /// <param name="migrators">The client migrators.</param>
    /// <returns>Awaitable task.</returns>
    internal static async Task Seed(IEventTypesStorage storage, IEventTypes eventTypes, IJsonSchemaGenerator schemas, IEventTypeMigrators migrators)
    {
        foreach (var group in eventTypes.All.GroupBy(type => type.Id))
        {
            var generations = group.Select(type => new KernelEvents::EventTypeGenerationDefinition(
                new KernelEvents::EventTypeGeneration(type.Generation.Value),
                schemas.Generate(eventTypes.GetClrTypeFor(type.Id, type.Generation)))).ToList();
            var migrations = group.SelectMany(type => migrators.GetMigratorsFor(eventTypes.GetClrTypeFor(type.Id, type.Generation)))
                .Select(CreateMigration).ToArray();
            foreach (var generation in migrations.SelectMany(migration => new[] { migration.FromGeneration, migration.ToGeneration }).Distinct()
                .Where(generation => generations.TrueForAll(existing => existing.Generation != generation)))
            {
                generations.Add(new(generation, new JsonSchema()));
            }

            var definition = new KernelEvents::EventTypeDefinition(
                new KernelEvents::EventTypeId(group.Key.Value), KernelEvents::EventTypeOwner.Client, false, generations, migrations);
            await storage.Register(definition);
            await storage.RecordMigrationsVersion(definition.Id, KernelEvents::EventTypeMigrationsVersion.For(migrations), migrations);
        }
    }

    static KernelEvents::EventTypeMigrationDefinition CreateMigration(IEventTypeMigration migration)
    {
        var upcast = new EventMigrationBuilder(new CamelCaseNamingPolicy());
        migration.Upcast(upcast);
        var downcast = new EventMigrationBuilder(new CamelCaseNamingPolicy());
        migration.Downcast(downcast);
        return new(new(migration.From.Value), new(migration.To.Value), [], upcast.ToJson(), downcast.ToJson());
    }
}

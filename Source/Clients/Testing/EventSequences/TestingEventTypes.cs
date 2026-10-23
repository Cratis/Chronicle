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
/// <remarks>
/// Each event type is seeded with the visibility the client registers it with, so the in-process kernel refuses a
/// public event type on the event log and a private one on the outbox exactly like a deployed kernel does.
/// </remarks>
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
                schemas.Generate(eventTypes.GetClrTypeFor(type.Id, type.Generation), eventTypes))).ToList();
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
            var latest = group.MaxBy(type => type.Generation.Value)!;
            var latestGeneration = generations.First(generation => generation.Generation.Value == latest.Generation.Value);
            await storage.Register(
                new KernelEvents::EventType(definition.Id, latestGeneration.Generation, latest.Tombstone),
                latestGeneration.Schema,
                KernelEvents::EventTypeOwner.Client,
                KernelEvents::EventTypeSource.Code,
                RegistrationVisibilityFor(eventTypes, latest));
            await storage.RecordMigrationsVersion(definition.Id, KernelEvents::EventTypeMigrationsVersion.For(migrations), migrations);
        }
    }

    static KernelEvents::EventTypeVisibility RegistrationVisibilityFor(IEventTypes eventTypes, EventType type) =>
        eventTypes is EventTypes registry
            ? (KernelEvents::EventTypeVisibility)(int)registry.GetDescriptorFor(eventTypes.GetClrTypeFor(type.Id, type.Generation)).RegistrationVisibility
            : KernelEvents::EventTypeVisibility.Unspecified;

    static KernelEvents::EventTypeMigrationDefinition CreateMigration(IEventTypeMigration migration)
    {
        var upcast = new EventMigrationBuilder(new CamelCaseNamingPolicy());
        migration.Upcast(upcast);
        var downcast = new EventMigrationBuilder(new CamelCaseNamingPolicy());
        migration.Downcast(downcast);
        return new(new(migration.From.Value), new(migration.To.Value), [], upcast.ToJson(), downcast.ToJson());
    }
}

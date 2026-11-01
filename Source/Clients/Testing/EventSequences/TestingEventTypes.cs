// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Testing.Events;
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
    /// <param name="metadata">The shared application metadata that remembers the definitions, or <see langword="null"/> to compute them.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The definitions are templates when they come from shared metadata, so every store registers copies of them and
    /// registers the whole set as one batch.
    /// </remarks>
    internal static async Task Seed(IEventTypesStorage storage, IEventTypes eventTypes, IJsonSchemaGenerator schemas, IEventTypeMigrators migrators, TestingArtifactCatalog? metadata = null)
    {
        if (metadata is not null && storage is DiscoveredEventTypesStorage discovered)
        {
            discovered.Seed(metadata.GetSeed(() => new EventTypeSeed(Registrations(eventTypes, schemas, migrators))));
            return;
        }

        var definitions = Registrations(eventTypes, schemas, migrators).ToArray();
        await storage.Register(definitions);
        foreach (var registration in definitions)
        {
            var definition = registration.Definition;
            await storage.RecordMigrationsVersion(definition.Id, KernelEvents::EventTypeMigrationsVersion.For(definition.Migrations), definition.Migrations);
        }
    }

    static IEnumerable<KernelEvents::EventTypeToRegister> Registrations(IEventTypes eventTypes, IJsonSchemaGenerator schemas, IEventTypeMigrators migrators)
    {
        var latestById = eventTypes.All.GroupBy(type => type.Id.Value).ToDictionary(group => group.Key, group => group.MaxBy(type => type.Generation.Value)!);
        return CreateDefinitions(eventTypes, schemas, migrators).Select(definition => WithVisibility(eventTypes, definition, latestById[definition.Id.Value]));
    }

    static KernelEvents::EventTypeToRegister WithVisibility(IEventTypes eventTypes, KernelEvents::EventTypeDefinition definition, EventType latest) =>
        new(definition with { Tombstone = latest.Tombstone }, KernelEvents::EventTypeSource.Code, RegistrationVisibilityFor(eventTypes, latest));

    static KernelEvents::EventTypeVisibility RegistrationVisibilityFor(IEventTypes eventTypes, EventType type) =>
        eventTypes is EventTypes registry
            ? (KernelEvents::EventTypeVisibility)(int)registry.GetDescriptorFor(eventTypes.GetClrTypeFor(type.Id, type.Generation)).RegistrationVisibility
            : KernelEvents::EventTypeVisibility.Unspecified;

    static List<KernelEvents::EventTypeDefinition> CreateDefinitions(IEventTypes eventTypes, IJsonSchemaGenerator schemas, IEventTypeMigrators migrators)
    {
        var definitions = new List<KernelEvents::EventTypeDefinition>();
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

            definitions.Add(new KernelEvents::EventTypeDefinition(
                new KernelEvents::EventTypeId(group.Key.Value), KernelEvents::EventTypeOwner.Client, false, generations, migrations));
        }

        return definitions;
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

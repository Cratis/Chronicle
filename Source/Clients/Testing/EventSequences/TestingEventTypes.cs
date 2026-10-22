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
    internal static async Task Seed(IEventTypesStorage storage, IEventTypes eventTypes, IJsonSchemaGenerator schemas, IEventTypeMigrators migrators, ApplicationMetadata? metadata = null)
    {
        var templates = metadata is null
            ? CreateDefinitions(eventTypes, schemas, migrators)
            : metadata.GetSeedDefinitions(() => CreateDefinitions(eventTypes, schemas, migrators));
        var definitions = templates.Select(Copy).ToArray();

        await storage.Register(definitions.Select(definition => new KernelEvents::EventTypeToRegister(definition, KernelEvents::EventTypeSource.Code)).ToArray());
        foreach (var definition in definitions)
        {
            await storage.RecordMigrationsVersion(definition.Id, KernelEvents::EventTypeMigrationsVersion.For(definition.Migrations), definition.Migrations);
        }
    }

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

    static KernelEvents::EventTypeDefinition Copy(KernelEvents::EventTypeDefinition definition) => definition with
    {
        Generations = definition.Generations
            .Select(generation => generation with { Schema = new JsonSchema(generation.Schema.Node.DeepClone().AsObject()) })
            .ToArray(),
        Migrations = definition.Migrations.Select(migration => migration with
        {
            Operations = migration.Operations.ToArray(),
            UpcastJmesPath = migration.UpcastJmesPath.DeepClone().AsObject(),
            DowncastJmesPath = migration.DowncastJmesPath.DeepClone().AsObject()
        }).ToArray()
    };

    static KernelEvents::EventTypeMigrationDefinition CreateMigration(IEventTypeMigration migration)
    {
        var upcast = new EventMigrationBuilder(new CamelCaseNamingPolicy());
        migration.Upcast(upcast);
        var downcast = new EventMigrationBuilder(new CamelCaseNamingPolicy());
        migration.Downcast(downcast);
        return new(new(migration.From.Value), new(migration.To.Value), [], upcast.ToJson(), downcast.ToJson());
    }
}

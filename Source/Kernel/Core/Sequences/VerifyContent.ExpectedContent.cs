// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Sequences;

public partial record VerifyContent
{
    static EventTypeGeneration? GetAppendedGeneration(Concepts.Events.AppendedEvent stored, StoredEventGenerations? history)
    {
        var appended = history?.AppendedGeneration ?? stored.Context.AppendedGeneration;
        if (appended is not null)
        {
            return appended;
        }

        var sources = history?.DerivedGenerations.Values.Where(_ => _.SourceIsAppended).Select(_ => _.Source).Distinct().ToArray() ?? [];
        return sources.Length == 1 ? sources[0] : null;
    }

    bool CanProveOriginalDifference(EventTypeGeneration appended, StoredEventGenerations? history) =>
        EventType.Generation == appended.Value || RequestProvenance(appended, history) is not null;

    GenerationProvenance? RequestProvenance(EventTypeGeneration appended, StoredEventGenerations? history) =>
        history?.DerivedGenerations.TryGetValue(new(EventType.Generation), out var provenance) == true &&
        provenance.SourceIsAppended && provenance.Source == appended ? provenance : null;

    async Task<Dictionary<int, EventTypeDefinition>?> GetDefinitions(IEventTypesStorage eventTypes, StoredEventGenerations? history, EventTypeGeneration appended, IEnumerable<int> generations)
    {
        var definition = await eventTypes.GetDefinition(EventType.Id);
        var provenance = history?.DerivedGenerations ?? new Dictionary<EventTypeGeneration, GenerationProvenance>();
        var versions = provenance.Count > 0 ? await eventTypes.GetMigrationsVersions(EventType.Id) : new Dictionary<EventTypeMigrationsVersion, IEnumerable<EventTypeMigrationDefinition>>();
        var result = new Dictionary<int, EventTypeDefinition>();
        foreach (var generation in generations)
        {
            var recorded = generation == appended.Value
                ? RequestProvenance(appended, history)
                : provenance.GetValueOrDefault(new((uint)generation));
            var selected = SelectDefinition(definition, recorded, versions);
            if (selected is null)
            {
                return null;
            }
            result[generation] = selected;
        }
        return result;
    }

    static EventTypeDefinition? SelectDefinition(EventTypeDefinition current, GenerationProvenance? recorded, IReadOnlyDictionary<EventTypeMigrationsVersion, IEnumerable<EventTypeMigrationDefinition>> versions)
    {
        if (recorded is null)
        {
            return current;
        }
        return versions.TryGetValue(recorded.MigrationsVersion, out var migrations) ? current with { Migrations = migrations } : null;
    }

    sealed class ExpectedContent(VerifyContent request, IEventTypeMigrations migrations, IExpandoObjectConverter converter)
    {
        readonly Dictionary<EventTypeMigrationsVersion, IDictionary<EventTypeGeneration, ExpandoObject>> _migrated = [];

        public async Task<IReadOnlyDictionary<int, (JsonSchema Schema, JsonObject Content)>?> Prepare(
            JsonObject masked,
            Dictionary<int, JsonSchema> schemas,
            Dictionary<int, EventTypeDefinition> definitions,
            IEventSequenceStorage sequence)
        {
            var result = new Dictionary<int, (JsonSchema Schema, JsonObject Content)>();
            foreach (var (generation, schema) in schemas)
            {
                var content = await GetGeneration(masked, schemas[(int)request.EventType.Generation], definitions[generation], new((uint)generation));
                if (content is null || ContentComparison.Prepare(content, schema, converter, sequence) is not { } prepared)
                {
                    return null;
                }
                result[generation] = (schema, prepared);
            }
            return result;
        }

        async Task<ExpandoObject?> GetGeneration(
            JsonObject masked,
            JsonSchema sourceSchema,
            EventTypeDefinition definition,
            EventTypeGeneration generation)
        {
            if (generation.Value == request.EventType.Generation)
            {
                var source = converter.ToExpandoObject(masked, sourceSchema);
                return ContentComparison.PreservesConversion(masked, source, sourceSchema) ? source : null;
            }

            var version = EventTypeMigrationsVersion.For(definition.Migrations);
            if (!_migrated.TryGetValue(version, out var content))
            {
                content = await Migrate(masked, sourceSchema, definition);
                if (content is null)
                {
                    return null;
                }
                _migrated[version] = content;
            }
            return content.TryGetValue(generation, out var value) ? value : null;
        }

        async Task<IDictionary<EventTypeGeneration, ExpandoObject>?> Migrate(JsonObject masked, JsonSchema schema, EventTypeDefinition definition)
        {
            var source = converter.ToExpandoObject(masked, schema);
            var lossless = ContentComparison.PreservesConversion(masked, source, schema);
            var inspected = new HashSet<ExpandoObject>(ReferenceEqualityComparer.Instance);
            var opaque = true;
            var result = await migrations.MigrateToAllGenerations(
                definition,
                new(request.EventType.Id, request.EventType.Generation, request.EventType.Tombstone),
                masked,
                source,
                (raw, generationSchema, converted) =>
                {
                    lossless &= ContentComparison.PreservesConversion(raw, converted, generationSchema);
                    inspected.Add(converted);
                },
                (operations, input) => opaque &= MigrationProvenance.CarriesProtectedValuesOpaquely(operations, input));

            // Append migrates plaintext; verification must keep protected values opaque as well as lossless.
            return opaque && lossless && result.Values.All(inspected.Contains) ? result : null;
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Adds missing generations using protected base content, never revision-delivery content.
/// </summary>
/// <param name="storage">The storage for event definitions and sequences.</param>
/// <param name="migrations">The plaintext migration engine.</param>
/// <param name="metadataManager">Generation-specific release and protection.</param>
/// <param name="converter">The schema-guided content converter.</param>
/// <param name="hashCalculator">The content hash calculator.</param>
internal class EventTypeGenerationBackfill(
    IStorage storage,
    IEventTypeMigrations migrations,
    IJsonSchemaMetadataManager metadataManager,
    IExpandoObjectConverter converter,
    IEventHashCalculator hashCalculator)
{
    /// <summary>
    /// Backfills the namespace's event log with one definition snapshot for the run.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="eventTypeId">The type being backfilled.</param>
    /// <param name="logger">The job step's logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The job step result.</returns>
    internal async Task<JobStepResult> Perform(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventTypeId eventTypeId, ILogger logger, CancellationToken cancellationToken)
    {
        var store = storage.GetEventStore(eventStore);
        var definition = await store.EventTypes.GetDefinition(eventTypeId);
        var version = EventTypeMigrationsVersion.For(definition.Migrations);
        await store.EventTypes.RecordMigrationsVersion(eventTypeId, version, definition.Migrations);
        var sequence = store.GetNamespace(@namespace).GetEventSequence(WellKnownEventSequences.EventLog);
        var protectedMigrations = new ProtectedEventTypeMigrations(store.EventTypes, migrations, metadataManager, converter);
        using var cursor = await sequence.GetFromSequenceNumber(EventSequenceNumber.First, eventTypes: [new(eventTypeId, EventTypeGeneration.First)], cancellationToken: cancellationToken);
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult());
                }

                await BackfillEvent(sequence, protectedMigrations, definition, version, eventStore, @namespace, @event.Context.SequenceNumber, logger);
            }
        }

        return JobStepResult.Succeeded(null);
    }

    async Task BackfillEvent(
        IEventSequenceStorage sequence,
        ProtectedEventTypeMigrations protectedMigrations,
        EventTypeDefinition definition,
        EventTypeMigrationsVersion version,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceNumber number,
        ILogger logger)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var observed = await sequence.GetStoredGenerations(number);
            if (observed is null || observed.EventTypeId != definition.Id || observed.Content.Count == 0)
            {
                return;
            }

            var missing = definition.Generations.Select(_ => _.Generation).Except(observed.Content.Keys).ToHashSet();
            if (missing.Count == 0)
            {
                return;
            }

            var sourceIsAppended = observed.AppendedGeneration is { } appended && observed.Content.ContainsKey(appended);
            var source = sourceIsAppended ? observed.AppendedGeneration! : observed.Content.Keys.MaxBy(_ => _.Value)!;
            if (!sourceIsAppended && observed.AppendedGeneration is not null)
            {
                logger.AppendedContentMissing(number, observed.AppendedGeneration);
            }

            var json = JsonNode.Parse(observed.Content[source])!.AsObject();
            var schema = definition.Generations.Single(_ => _.Generation == source).Schema;
            var protectedEvent = converter.ToExpandoObject(json, schema);
            var subject = observed.Subject.IsSet ? observed.Subject.Value : observed.EventSourceId.Value;
            var content = await protectedMigrations.Migrate(eventStore, @namespace, new EventType(definition.Id, source), json, protectedEvent, subject, definition);
            var provenance = new GenerationProvenance(source, sourceIsAppended, version);
            var additions = content.Where(_ => missing.Contains(_.Key)).Select(_ => new GenerationToAdd(
                _.Key, _.Value, hashCalculator.Calculate(definition.Id, observed.EventSourceId, _.Value), provenance)).ToArray();
            if (await sequence.TryAddGenerations(observed, additions))
            {
                return;
            }
        }

        logger.BackfillConflicted(number, definition.Id);
    }
}

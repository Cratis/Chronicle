// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Verifies complete stored content without sending released content back to the caller.
/// </summary>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="SequenceNumber">The exact event sequence number.</param>
/// <param name="EventType">The attempted event type and generation.</param>
/// <param name="Content">The prepared append's plaintext JSON, as sent by append.</param>
/// <param name="EventSourceId">Optional event source to require.</param>
/// <remarks>
/// This read-only operation is a command so plaintext travels in a request body, never in a query URL.
/// </remarks>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record VerifyContent(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    Concepts.EventSequences.EventSequenceId EventSequenceId,
    EventSequenceNumber SequenceNumber,
    EventType EventType,
    string Content,
    string? EventSourceId = default)
{
    /// <summary>
    /// Compares every stored generation after migration and strict compliance release.
    /// </summary>
    /// <param name="storage">The storage to read.</param>
    /// <param name="metadataManager">The strict release implementation.</param>
    /// <param name="expandoObjectConverter">The schema-guided append and storage converter.</param>
    /// <param name="eventTypeMigrations">The append migration implementation.</param>
    /// <returns>Equal, different, or unavailable; partial release never produces equality.</returns>
    public async Task<ContentVerification> Handle(IStorage storage, IJsonSchemaMetadataManager metadataManager, IExpandoObjectConverter expandoObjectConverter, IEventTypeMigrations eventTypeMigrations)
    {
        var eventStore = storage.GetEventStore(EventStore);
        var sequence = eventStore.GetNamespace(Namespace).GetEventSequence(EventSequenceId);
        if (!sequence.SupportsRevisionTracking)
        {
            return new(ContentVerificationResult.Unavailable);
        }

        using var cursor = await sequence.GetRange(SequenceNumber, SequenceNumber);
        if (!await cursor.MoveNext())
        {
            return new(ContentVerificationResult.Unavailable);
        }

        var stored = cursor.Current.SingleOrDefault();
        if (stored is null || stored.Context.SequenceNumber != SequenceNumber ||
            (EventSourceId is not null && stored.Context.EventSourceId.Value != EventSourceId))
        {
            return new(ContentVerificationResult.Unavailable);
        }

        if (stored.IsRevised || stored.Context.EventType.Id == GlobalEventTypes.Redaction)
        {
            return new(ContentVerificationResult.Unavailable);
        }

        if (stored.Context.EventType.Id.Value != EventType.Id)
        {
            return new(ContentVerificationResult.Different);
        }

        if (!stored.GenerationalContent.ContainsKey((int)EventType.Generation) ||
            !await eventStore.EventTypes.HasFor(EventType.Id, EventType.Generation))
        {
            return new(ContentVerificationResult.Unavailable);
        }

        var schema = await eventStore.EventTypes.GetFor(EventType.Id, EventType.Generation);
        try
        {
            if (JsonNode.Parse(Content) is not JsonObject attempted)
            {
                return new(ContentVerificationResult.Unavailable);
            }

            var schemas = new Dictionary<int, JsonSchema>();
            foreach (var generation in stored.GenerationalContent.Keys)
            {
                if (generation <= 0 || !await eventStore.EventTypes.HasFor(EventType.Id, (uint)generation))
                {
                    return new(ContentVerificationResult.Unavailable);
                }

                schemas[generation] = (await eventStore.EventTypes.GetFor(EventType.Id, (uint)generation)).Schema;
            }

            var expected = await metadataManager.TryPrepareGenerationsForComparison(schema.Schema, attempted, async masked =>
            {
                var source = expandoObjectConverter.ToExpandoObject(masked, schema.Schema);
                var lossless = ContentComparison.PreservesConversion(masked, source, schema.Schema);
                var inspected = new HashSet<ExpandoObject>(ReferenceEqualityComparer.Instance);
                var opaque = true;
                var migrated = await eventTypeMigrations.MigrateToAllGenerations(
                    EventStore,
                    new(EventType.Id, EventType.Generation, EventType.Tombstone),
                    masked,
                    source,
                    (raw, generationSchema, converted) =>
                    {
                        lossless &= ContentComparison.PreservesConversion(raw, converted, generationSchema);
                        inspected.Add(converted);
                    },
                    (operations, input) => opaque &= MigrationProvenance.CarriesProtectedValuesOpaquely(operations, input));

                // The real append migrates ciphertext: a migration that reads a protected value other than to rename,
                // move or copy it whole cannot be reproduced on a marker, whether or not the marker survived.
                if (!opaque || !lossless || migrated.Values.Any(content => !inspected.Contains(content)))
                {
                    return null;
                }

                var result = new Dictionary<int, (JsonSchema Schema, JsonObject Content)>();
                foreach (var (generation, generationSchema) in schemas)
                {
                    if (!migrated.TryGetValue((uint)generation, out var content))
                    {
                        return null;
                    }

                    var prepared = ContentComparison.Prepare(content, generationSchema, expandoObjectConverter, sequence);
                    if (prepared is null)
                    {
                        return null;
                    }

                    result[generation] = (generationSchema, prepared);
                }

                return result;
            });
            if (expected is null)
            {
                return new(ContentVerificationResult.Unavailable);
            }

            var equal = true;
            foreach (var (generation, content) in stored.GenerationalContent)
            {
                if (JsonNode.Parse(content) is not JsonObject document ||
                    await metadataManager.TryRelease(EventStore, Namespace, schemas[generation], stored.Context.Subject.Value, document) is not { } released ||
                    VerificationMarkers.AppearIn(released))
                {
                    // Stored text that looks like a verification marker cannot be told apart from one: never compare it.
                    return new(ContentVerificationResult.Unavailable);
                }

                // Finish every release before returning Different: a partially compared event is unavailable.
                equal &= ContentComparison.Equals(released, expected[generation]);
            }

            return new(equal ? ContentVerificationResult.Equal : ContentVerificationResult.Different);
        }
        catch (Exception exception) when (exception is JsonException or FormatException or InvalidCastException or InvalidOperationException or OverflowException)
        {
            // Content that cannot be represented by the generation schema is not evidence of equality.
            return new(ContentVerificationResult.Unavailable);
        }
    }
}

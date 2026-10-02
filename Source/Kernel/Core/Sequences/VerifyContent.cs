// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
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
    /// Compares JSON in the requested stored generation after strict compliance release.
    /// </summary>
    /// <param name="storage">The storage to read.</param>
    /// <param name="metadataManager">The strict release implementation.</param>
    /// <param name="expandoObjectConverter">The schema-guided append and storage converter.</param>
    /// <returns>Equal, different, or unavailable; partial release never produces equality.</returns>
    public async Task<ContentVerification> Handle(IStorage storage, IJsonSchemaMetadataManager metadataManager, IExpandoObjectConverter expandoObjectConverter)
    {
        var eventStore = storage.GetEventStore(EventStore);
        var sequence = eventStore.GetNamespace(Namespace).GetEventSequence(EventSequenceId);
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

        if (stored.Context.EventType.Id == GlobalEventTypes.Redaction)
        {
            return new(ContentVerificationResult.Unavailable);
        }

        if (stored.Context.EventType.Id.Value != EventType.Id)
        {
            return new(ContentVerificationResult.Different);
        }

        // Revisions are not represented consistently across storage backends. Never compare stale
        // original content as if it were the current revision, or silently upcast another generation.
        if (stored.IsRevised ||
            !stored.GenerationalContent.TryGetValue((int)EventType.Generation, out var content) ||
            !await eventStore.EventTypes.HasFor(EventType.Id, EventType.Generation))
        {
            return new(ContentVerificationResult.Unavailable);
        }

        var schema = await eventStore.EventTypes.GetFor(EventType.Id, EventType.Generation);
        try
        {
            if (JsonNode.Parse(content) is not JsonObject document || JsonNode.Parse(Content) is not JsonObject attempted)
            {
                return new(ContentVerificationResult.Unavailable);
            }

            var released = await metadataManager.TryRelease(EventStore, Namespace, schema.Schema, stored.Context.Subject.Value, document);
            if (released is null)
            {
                return new(ContentVerificationResult.Unavailable);
            }

            return new(ContentComparison.Equals(released, attempted, schema.Schema, expandoObjectConverter) ? ContentVerificationResult.Equal : ContentVerificationResult.Different);
        }
        catch (Exception exception) when (exception is JsonException or FormatException or InvalidCastException or InvalidOperationException or OverflowException)
        {
            // Content that cannot be represented by the generation schema is not evidence of equality.
            return new(ContentVerificationResult.Unavailable);
        }
    }
}

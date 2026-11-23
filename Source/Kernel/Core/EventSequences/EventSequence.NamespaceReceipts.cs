// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    /// <inheritdoc/>
    public Task<AppendResult> AppendPublicationToNamespace(NamespaceIncarnationId expectedIncarnation, string publicationId, string intentFingerprint, EventToAppend @event, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy) =>
        InNamespace(expectedIncarnation, () => AppendPublication(publicationId, intentFingerprint, @event, correlationId, causation, causedBy));

    /// <inheritdoc/>
    public Task<IdempotentAppendResult> AppendWithIdempotencyKeyToNamespace(NamespaceIncarnationId expectedIncarnation, EventToAppend @event, IdempotencyKey idempotencyKey, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy, ConcurrencyScope concurrencyScope) =>
        InNamespace(expectedIncarnation, () => AppendWithIdempotencyKey(@event, idempotencyKey, correlationId, causation, causedBy, concurrencyScope));

    /// <inheritdoc/>
    public Task<IdempotentAppendManyResult> AppendManyWithIdempotencyKeysToNamespace(NamespaceIncarnationId expectedIncarnation, IEnumerable<EventToAppend> events, IReadOnlyList<IdempotencyKey?> idempotencyKeys, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy, ConcurrencyScopes concurrencyScopes) =>
        InNamespace(expectedIncarnation, async () =>
        {
            var materialized = events.ToArray();

            // An empty keyed batch still needs a fenced operation before it can report success.
            if (materialized.Length == 0 && idempotencyKeys.Count == 0) await PublicationStorage.TryGetPublications([]);
            return await AppendManyWithIdempotencyKeys(materialized, idempotencyKeys, correlationId, causation, causedBy, concurrencyScopes);
        });

    /// <inheritdoc/>
    public Task<AppendManyResult> AppendManyWithEffectReceiptToNamespace(NamespaceIncarnationId expectedIncarnation, IEnumerable<EventToAppend> events, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy, ConcurrencyScopes concurrencyScopes, Occurrences.EffectReceipt effectReceipt) =>
        InNamespace(expectedIncarnation, () => AppendManyWithEffectReceipt(events, correlationId, causation, causedBy, concurrencyScopes, effectReceipt));

    /// <inheritdoc/>
    public Task<AppendManyResult> AppendManyWithConjunctionsToNamespace(NamespaceIncarnationId expectedIncarnation, IEnumerable<EventToAppend> events, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy, ConcurrencyScopeConjunctions concurrencyScopes) =>
        InNamespace(expectedIncarnation, () => AppendManyWithConjunctions(events, correlationId, causation, causedBy, concurrencyScopes));

    /// <inheritdoc/>
    public Task<AppendManyResult> AppendManyWithConjunctionsAndEffectReceiptToNamespace(NamespaceIncarnationId expectedIncarnation, IEnumerable<EventToAppend> events, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy, ConcurrencyScopeConjunctions concurrencyScopes, Occurrences.EffectReceipt effectReceipt) =>
        InNamespace(expectedIncarnation, () => AppendManyWithConjunctionsAndEffectReceipt(events, correlationId, causation, causedBy, concurrencyScopes, effectReceipt));

    /// <inheritdoc/>
    public Task<Occurrences.RecordEffectReceiptResult> RecordEffectReceiptToNamespace(NamespaceIncarnationId expectedIncarnation, Occurrences.EffectReceipt effectReceipt, EffectOutcome outcome, IReadOnlyList<string> reasons) =>
        InNamespace(expectedIncarnation, () => RecordEffectReceipt(effectReceipt, outcome, reasons));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.EventSequences;

public partial interface IEventSequence
{
    /// <summary>Appends a durable publication only in the caller's expected namespace incarnation.</summary>
    /// <param name="expectedIncarnation">The caller's fixed namespace identity, retained across retries.</param>
    /// <param name="publicationId">The opaque deterministic identity persisted with the intent, scoped to this destination.</param>
    /// <param name="intentFingerprint">The persisted digest of the plaintext intent and all append context.</param>
    /// <param name="event">The event from the durable intent.</param>
    /// <param name="correlationId">The original correlation identity.</param>
    /// <param name="causation">The original causation chain.</param>
    /// <param name="causedBy">The original identity chain.</param>
    /// <returns>The existing or newly committed sequence number, or an explicit append failure.</returns>
    Task<AppendResult> AppendPublicationToNamespace(NamespaceIncarnationId expectedIncarnation, string publicationId, string intentFingerprint, EventToAppend @event, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy);

    /// <summary>Appends one event with an idempotency key only in the caller's expected namespace incarnation.</summary>
    /// <param name="expectedIncarnation">The caller's fixed namespace identity, retained across retries.</param>
    /// <param name="event">The event to append.</param>
    /// <param name="idempotencyKey">The key identifying the append intent.</param>
    /// <param name="correlationId">The attempt's correlation identifier.</param>
    /// <param name="causation">The causation chain.</param>
    /// <param name="causedBy">The identity causing the append.</param>
    /// <param name="concurrencyScope">The concurrency scope, checked only for a new append.</param>
    /// <returns>The append settlement.</returns>
    Task<IdempotentAppendResult> AppendWithIdempotencyKeyToNamespace(NamespaceIncarnationId expectedIncarnation, EventToAppend @event, IdempotencyKey idempotencyKey, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy, ConcurrencyScope concurrencyScope);

    /// <summary>Appends a batch with optional idempotency keys only in the caller's expected namespace incarnation.</summary>
    /// <param name="expectedIncarnation">The caller's fixed namespace identity, retained across retries.</param>
    /// <param name="events">The requested events, in order.</param>
    /// <param name="idempotencyKeys">One optional key per event, in the same order.</param>
    /// <param name="correlationId">The attempt's correlation identifier.</param>
    /// <param name="causation">The causation chain.</param>
    /// <param name="causedBy">The identity causing the append.</param>
    /// <param name="concurrencyScopes">The scopes checked for the new remainder only.</param>
    /// <returns>The batch settlement, with sequence numbers and outcomes in request order on success.</returns>
    Task<IdempotentAppendManyResult> AppendManyWithIdempotencyKeysToNamespace(NamespaceIncarnationId expectedIncarnation, IEnumerable<EventToAppend> events, IReadOnlyList<IdempotencyKey?> idempotencyKeys, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy, ConcurrencyScopes concurrencyScopes);

    /// <summary>Appends a batch with an effect receipt only in the caller's expected namespace incarnation.</summary>
    /// <param name="expectedIncarnation">The caller's fixed namespace identity, retained across retries.</param>
    /// <param name="events">The events; empty for a protected validate-only decision.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="causation">The causation chain.</param>
    /// <param name="causedBy">The identity that caused the append.</param>
    /// <param name="concurrencyScopes">The protected decision scopes.</param>
    /// <param name="effectReceipt">The occurrence effect and optional response.</param>
    /// <returns>The append result or the winning receipt.</returns>
    Task<AppendManyResult> AppendManyWithEffectReceiptToNamespace(NamespaceIncarnationId expectedIncarnation, IEnumerable<EventToAppend> events, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy, ConcurrencyScopes concurrencyScopes, Occurrences.EffectReceipt effectReceipt);

    /// <summary>Appends a batch guarded by concurrency conjunctions only in the caller's expected namespace incarnation.</summary>
    /// <param name="expectedIncarnation">The caller's fixed namespace identity, retained across retries.</param>
    /// <param name="events">The events.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="causation">The causation chain.</param>
    /// <param name="causedBy">The identity.</param>
    /// <param name="concurrencyScopes">The ordered guards.</param>
    /// <returns>The append result.</returns>
    Task<AppendManyResult> AppendManyWithConjunctionsToNamespace(NamespaceIncarnationId expectedIncarnation, IEnumerable<EventToAppend> events, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy, ConcurrencyScopeConjunctions concurrencyScopes);

    /// <summary>Appends a batch guarded by concurrency conjunctions with an effect receipt only in the caller's expected namespace incarnation.</summary>
    /// <param name="expectedIncarnation">The caller's fixed namespace identity, retained across retries.</param>
    /// <param name="events">The events.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="causation">The causation chain.</param>
    /// <param name="causedBy">The identity.</param>
    /// <param name="concurrencyScopes">The ordered guards.</param>
    /// <param name="effectReceipt">The receipt to commit atomically.</param>
    /// <returns>The append result.</returns>
    Task<AppendManyResult> AppendManyWithConjunctionsAndEffectReceiptToNamespace(NamespaceIncarnationId expectedIncarnation, IEnumerable<EventToAppend> events, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy, ConcurrencyScopeConjunctions concurrencyScopes, Occurrences.EffectReceipt effectReceipt);

    /// <summary>Records an effect receipt without events only in the caller's expected namespace incarnation.</summary>
    /// <param name="expectedIncarnation">The caller's fixed namespace identity, retained across retries.</param>
    /// <param name="effectReceipt">The occurrence effect and optional response.</param>
    /// <param name="outcome">The NoEvents or Refused outcome.</param>
    /// <param name="reasons">The refusal reasons.</param>
    /// <returns>The winning receipt and whether this call recorded it.</returns>
    Task<Occurrences.RecordEffectReceiptResult> RecordEffectReceiptToNamespace(NamespaceIncarnationId expectedIncarnation, Occurrences.EffectReceipt effectReceipt, EffectOutcome outcome, IReadOnlyList<string> reasons);
}

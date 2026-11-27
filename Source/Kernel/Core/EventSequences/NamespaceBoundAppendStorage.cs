// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Occurrences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Binds validated append and receipt operations to one immutable namespace commit context.
/// </summary>
/// <param name="storage">The provider's transactional namespace-fence capability.</param>
/// <param name="context">The caller's expected incarnation and captured activation lifetime.</param>
/// <remarks>
/// A binding never issues or refreshes an incarnation and never falls back to tokenless storage.
/// Empty operations and receipt replays are forwarded to the provider's fence like writes.
/// Provider failures, including unknown commits, propagate unchanged for the append pipeline to settle.
/// </remarks>
public sealed class NamespaceBoundAppendStorage(INamespaceFencedEventSequenceStorage storage, NamespaceCommitContext context) : IEventPublicationStorage, IEffectReceiptStorage
{
    /// <summary>
    /// Appends one validated event under the bound namespace fence.
    /// </summary>
    /// <param name="event">The validated, protected event.</param>
    /// <returns>The durable event or a sequence-slot collision.</returns>
    public Task<Result<AppendedEvent, DuplicateEventSequenceNumber>> Append(EventToAppendToStorage @event) => storage.Append(context, @event);

    /// <summary>
    /// Appends validated events atomically under the bound namespace fence.
    /// </summary>
    /// <param name="events">The validated, protected events, including named tags.</param>
    /// <returns>The durable events or a sequence-slot collision.</returns>
    public Task<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>> AppendMany(IEnumerable<EventToAppendToStorage> events) => storage.AppendMany(context, events);

    /// <inheritdoc/>
    public Task<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>> AppendManyWithIdentities(IEnumerable<EventToAppendToStorage> events) => storage.AppendManyWithIdentities(context, events);

    /// <inheritdoc/>
    public Task<Option<EventPublicationReceipt>> TryGetPublication(EventPublication publication) => storage.TryGetPublication(context, publication);

    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<string, StoredEventPublication>> TryGetPublications(IEnumerable<string> identities) => storage.TryGetPublications(context, identities);

    /// <inheritdoc/>
    public Task<Result<EventPublicationReceipt, DuplicateEventSequenceNumber>> AppendPublication(EventPublication publication, EventToAppendToStorage @event) => storage.AppendPublication(context, publication, @event);

    /// <inheritdoc/>
    public Task<EffectReceipt?> GetEffectReceipt(string id) => storage.GetEffectReceipt(context, id);

    /// <inheritdoc/>
    public Task<IEnumerable<EffectReceipt>> GetEffectReceipts(OccurrenceId occurrenceId) => storage.GetEffectReceipts(context, occurrenceId);

    /// <inheritdoc/>
    public Task<IEnumerable<EffectReceipt>> GetEffectReceiptsForOccurrences(IEnumerable<OccurrenceId> occurrenceIds) => storage.GetEffectReceiptsForOccurrences(context, occurrenceIds);

    /// <inheritdoc/>
    public Task<EffectReceiptWriteResult> AppendManyWithEffectReceipt(IEnumerable<EventToAppendToStorage> events, EffectReceipt receipt) => storage.AppendManyWithEffectReceipt(context, events, receipt);
}

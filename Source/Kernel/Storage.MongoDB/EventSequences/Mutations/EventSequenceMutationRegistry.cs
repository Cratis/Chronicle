// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using MongoDB.Driver;

using ProviderNeutralHistoryEntry = Cratis.Chronicle.Storage.EventSequences.Mutations.EventSequenceMutationHistoryEntry;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations;

/// <summary>
/// Represents a namespace-scoped MongoDB event sequence mutation registry.
/// </summary>
/// <param name="eventStore">The event store that owns the registry.</param>
/// <param name="namespace">The namespace that owns the registry.</param>
/// <param name="database">The <see cref="IEventStoreNamespaceDatabase"/> to persist to.</param>
/// <remarks>
/// Every operation is backed by MongoDB compare-and-swap document operations rather than an in-process lock. The
/// head document (keyed by <see cref="EventSequenceId"/>) carries at most one active mutation at a time; the
/// history collection (keyed by <see cref="EventSequenceMutationId"/>) carries the payload-free terminal receipt
/// for every mutation that has been archived. Because the terminal receipt never retains the original command
/// payload, retries verify the token binding and terminal receipt digest without reconstructing a command.
/// Archive writes the receipt before releasing the exact terminal head. Retries recover that release without
/// touching a later mutation, including after provider recreation or head reuse.
/// </remarks>
public class EventSequenceMutationRegistry(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    IEventStoreNamespaceDatabase database) : IEventSequenceMutationRegistry
{
    readonly IMongoCollection<EventSequenceMutationHeadEntry> _heads = database.GetCollection<EventSequenceMutationHeadEntry>(WellKnownCollectionNames.EventSequenceMutationHeads);
    readonly IMongoCollection<EventSequenceMutationHistoryEntry> _history = database.GetCollection<EventSequenceMutationHistoryEntry>(WellKnownCollectionNames.EventSequenceMutationHistory);

    /// <inheritdoc/>
    public async Task<EventSequenceMutationBeginResult> Begin(
        EventSequenceMutationRequest request,
        EventSequenceMutationTarget proposedTarget,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return await Task.FromCanceled<EventSequenceMutationBeginResult>(cancellationToken).ConfigureAwait(false);
        }

        var validation = EventSequenceMutationValidator.ValidateRequest(request);
        if (!validation.IsValid)
        {
            if (request?.Id is { } id && id != EventSequenceMutationId.NotSet)
            {
                var archived = await FindHistoryById(id, cancellationToken).ConfigureAwait(false);
                var bound = await _heads.Find(Builders<EventSequenceMutationHeadEntry>.Filter.Eq(_ => _.Active!.Definition.Request.Id, id))
                    .AnyAsync(cancellationToken).ConfigureAwait(false);
                if (archived is not null || bound)
                {
                    return EventSequenceMutationBeginResult.DefinitionConflict(id);
                }
            }

            return EventSequenceMutationBeginResult.Invalid(validation);
        }

        var scope = Scope(request.TargetSequence);

        var archivedReplay = await ResolveArchivedPermanentRecord(scope, request, cancellationToken).ConfigureAwait(false);
        if (archivedReplay is not null)
        {
            return archivedReplay;
        }

        var registeredHead = await _heads.Find(Builders<EventSequenceMutationHeadEntry>.Filter.Eq(_ => _.Active!.Definition.Request.Id, request.Id))
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (registeredHead is not null)
        {
            return ResolveBoundResume(scope, request, registeredHead);
        }

        var targetId = TargetId(request.TargetSequence);
        var head = await FindHead(targetId, cancellationToken).ConfigureAwait(false);
        if (head?.Active is not null)
        {
            return head.Active.Id == request.Id
                ? ResolveBoundResume(scope, request, head)
                : EventSequenceMutationBeginResult.MutationAlreadyInProgress(head.Active.Id);
        }

        // The head may have been archived between the first history read and the head read.
        archivedReplay = await ResolveArchivedPermanentRecord(scope, request, cancellationToken).ConfigureAwait(false);
        return archivedReplay ?? await ReserveNewMutation(scope, request, proposedTarget, head, targetId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<EventSequenceMutationRegistryTransitionResult> Transition(
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token,
        EventSequenceMutationTransition transition,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return await Task.FromCanceled<EventSequenceMutationRegistryTransitionResult>(cancellationToken).ConfigureAwait(false);
        }

        var validation = EventSequenceMutationValidator.ValidateIdentity(target);
        if (!validation.IsValid)
        {
            return EventSequenceMutationRegistryTransitionResult.Invalid(validation);
        }

        validation = EventSequenceMutationValidator.ValidateToken(token);
        if (!validation.IsValid)
        {
            return EventSequenceMutationRegistryTransitionResult.Invalid(validation);
        }

        var scope = Scope(target);
        var targetId = TargetId(target);
        var head = await FindHead(targetId, cancellationToken).ConfigureAwait(false);
        var archived = await FindHistoryById(token.Id, cancellationToken).ConfigureAwait(false);
        if (archived is not null)
        {
            return EventSequenceMutationRegistryTransitionResult.FromReceipt(scope, token, ToStorageHistory(archived));
        }

        if (head?.Active is not { } active || active.Id != token.Id ||
            !IsValidHead(scope, head.Coverage, head.LastAssignedOrdinal, active) ||
            !MatchesBinding(scope, target, token, active))
        {
            return EventSequenceMutationRegistryTransitionResult.StateConflict();
        }

        var result = EventSequenceMutationStateMachine.Apply(scope, active, transition, token);
        if (!result.Validation.IsValid)
        {
            return EventSequenceMutationRegistryTransitionResult.Invalid(result.Validation);
        }

        if (result.Outcome == EventSequenceMutationTransitionOutcome.Conflict)
        {
            return EventSequenceMutationRegistryTransitionResult.StateConflict();
        }

        if (result.Outcome == EventSequenceMutationTransitionOutcome.AlreadyApplied)
        {
            // The successor is byte-for-byte identical to the currently observed state, so there is nothing to
            // persist - skip the write and hand back the unchanged value directly.
            return EventSequenceMutationRegistryTransitionResult.AlreadyApplied(result.Mutation!, result.Token!);
        }

        if (result.Outcome != EventSequenceMutationTransitionOutcome.Applied)
        {
            return EventSequenceMutationRegistryTransitionResult.Corrupt();
        }

        var filter = ExactHead(targetId, active);
        var update = Builders<EventSequenceMutationHeadEntry>.Update.Set(_ => _.Active, result.Mutation);

        var updated = await _heads.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<EventSequenceMutationHeadEntry> { ReturnDocument = ReturnDocument.After },
            cancellationToken).ConfigureAwait(false);

        return updated is not null
            ? EventSequenceMutationRegistryTransitionResult.Applied(result.Mutation!, result.Token!)
            : EventSequenceMutationRegistryTransitionResult.StateConflict();
    }

    /// <inheritdoc/>
    public async Task<EventSequenceMutationRegistryArchiveResult> Archive(
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return await Task.FromCanceled<EventSequenceMutationRegistryArchiveResult>(cancellationToken).ConfigureAwait(false);
        }

        var validation = EventSequenceMutationValidator.ValidateIdentity(target);
        if (!validation.IsValid)
        {
            return EventSequenceMutationRegistryArchiveResult.Invalid(validation);
        }

        validation = EventSequenceMutationValidator.ValidateToken(token);
        if (!validation.IsValid)
        {
            return EventSequenceMutationRegistryArchiveResult.Invalid(validation);
        }

        var scope = Scope(target);
        var targetId = TargetId(target);
        var head = await FindHead(targetId, cancellationToken).ConfigureAwait(false);
        var archived = await FindHistoryById(token.Id, cancellationToken).ConfigureAwait(false);
        if (archived is not null)
        {
            return await RecoverArchive(scope, token, archived, cancellationToken).ConfigureAwait(false);
        }

        if (head?.Active is not { } active || active.Id != token.Id ||
            !IsValidHead(scope, head.Coverage, head.LastAssignedOrdinal, active) ||
            !MatchesBinding(scope, target, token, active))
        {
            return EventSequenceMutationRegistryArchiveResult.StateConflict();
        }

        var prepared = EventSequenceMutationStateMachine.PrepareArchive(scope, active, token);
        if (!prepared.Validation.IsValid)
        {
            return EventSequenceMutationRegistryArchiveResult.Invalid(prepared.Validation);
        }

        if (prepared.Outcome == EventSequenceMutationArchiveOutcome.Conflict)
        {
            return EventSequenceMutationRegistryArchiveResult.StateConflict();
        }

        if (prepared.Outcome != EventSequenceMutationArchiveOutcome.Prepared || prepared.History is null)
        {
            return EventSequenceMutationRegistryArchiveResult.Corrupt();
        }

        var history = prepared.History;
        var document = ToDocument(history, targetId);

        try
        {
            await _history.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            var existing = await FindHistoryById(token.Id, cancellationToken).ConfigureAwait(false);
            return existing is null
                ? EventSequenceMutationRegistryArchiveResult.Corrupt()
                : await RecoverArchive(scope, token, existing, cancellationToken).ConfigureAwait(false);
        }

        await ReleaseArchivedHead(scope, history, cancellationToken).ConfigureAwait(false);

        var registration = new EventSequenceMutationRegistration(
            active.Definition,
            EventSequenceMutationRegistryLifecycle.Archived,
            history.Ordinal,
            history.TerminalWitness);
        return EventSequenceMutationRegistryArchiveResult.Archived(scope, registration, history);
    }

    /// <inheritdoc/>
    public async Task<EventSequenceMutationTrackingResult> BeginTracking(
        EventSequenceMutationIdentity target,
        EventSequenceMutationCoverage expected,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return await Task.FromCanceled<EventSequenceMutationTrackingResult>(cancellationToken).ConfigureAwait(false);
        }

        var validation = EventSequenceMutationValidator.ValidateIdentity(target);
        if (!validation.IsValid)
        {
            return EventSequenceMutationTrackingResult.Invalid(validation);
        }

        validation = EventSequenceMutationValidator.ValidateTrackingCoverage(expected);
        if (!validation.IsValid)
        {
            return EventSequenceMutationTrackingResult.Invalid(validation);
        }

        var scope = Scope(target);
        validation = EventSequenceMutationValidator.ValidateScope(scope);
        if (!validation.IsValid)
        {
            return EventSequenceMutationTrackingResult.Invalid(validation);
        }

        var targetId = TargetId(target);
        var filter = Builders<EventSequenceMutationHeadEntry>.Filter.And(
            Builders<EventSequenceMutationHeadEntry>.Filter.Eq(_ => _.EventSequenceId, targetId),
            Builders<EventSequenceMutationHeadEntry>.Filter.Eq(_ => _.Coverage, expected));
        var update = Builders<EventSequenceMutationHeadEntry>.Update
            .Set(_ => _.Coverage, EventSequenceMutationCoverage.Unsealed)
            .SetOnInsert(_ => _.LastAssignedOrdinal, EventSequenceMutationOrdinal.NotSet);
        var options = new FindOneAndUpdateOptions<EventSequenceMutationHeadEntry> { IsUpsert = true, ReturnDocument = ReturnDocument.After };

        var updated = await TryFindOneAndUpdate(filter, update, options, cancellationToken).ConfigureAwait(false);
        if (updated is not null)
        {
            return EventSequenceMutationTrackingResult.Began();
        }

        var current = await FindHead(targetId, cancellationToken).ConfigureAwait(false);
        var currentCoverage = current?.Coverage ?? EventSequenceMutationCoverage.Untracked;
        return currentCoverage == EventSequenceMutationCoverage.Unsealed
            ? EventSequenceMutationTrackingResult.AlreadyTracking()
            : EventSequenceMutationTrackingResult.Conflict(currentCoverage);
    }

    static EventSequenceMutationBeginResult ResolveBoundResume(
        EventSequenceKey scope,
        EventSequenceMutationRequest request,
        EventSequenceMutationHeadEntry head)
    {
        var active = head.Active!;
        if (!active.Definition.Request.Equals(request))
        {
            return EventSequenceMutationBeginResult.DefinitionConflict(request.Id);
        }

        if (!IsValidHead(scope, head.Coverage, head.LastAssignedOrdinal, active))
        {
            return EventSequenceMutationBeginResult.Corrupt();
        }

        var registration = new EventSequenceMutationRegistration(
            active.Definition,
            EventSequenceMutationRegistryLifecycle.Bound,
            active.Ordinal,
            null);

        if (!registration.IsExactRequest(request))
        {
            return EventSequenceMutationBeginResult.DefinitionConflict(request.Id);
        }

        return EventSequenceMutationBeginResult.Resumed(active, EventSequenceMutationStateToken.Create(scope, active));
    }

    static EventSequenceId TargetId(EventSequenceMutationIdentity target) => target.Display;

    static ProviderNeutralHistoryEntry ToStorageHistory(EventSequenceMutationHistoryEntry document) =>
        new(document.MutationId, document.Ordinal, document.Origin, document.Kind, document.CommandHash, document.Target, document.RepairState, document.TerminalWitness);

    static EventSequenceMutationHistoryEntry ToDocument(ProviderNeutralHistoryEntry history, EventSequenceId targetId) =>
        new(history.Id, targetId, history.Ordinal, history.Origin, history.Kind, history.CommandHash, history.Target, history.RepairState, history.TerminalWitness);

    static bool IsValidHead(
        EventSequenceKey scope,
        EventSequenceMutationCoverage coverage,
        EventSequenceMutationOrdinal lastAssignedOrdinal,
        EventSequenceMutation? active) =>
        Enum.IsDefined(coverage) &&
        lastAssignedOrdinal is { Value: >= 0 } &&
        (active is null || (lastAssignedOrdinal == active.Ordinal && EventSequenceMutationValidator.ValidateActive(scope, active).IsValid));

    static bool MatchesBinding(
        EventSequenceKey scope,
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token,
        EventSequenceMutation active) =>
        token.Scope == scope &&
        token.TargetKey == target.Key &&
        active.TargetSequence == target &&
        token.Id == active.Id &&
        token.Ordinal == active.Ordinal &&
        token.DefinitionDigestV1 == active.Definition.DefinitionDigestV1;

    static FilterDefinition<EventSequenceMutationHeadEntry> ExactHead(EventSequenceId targetId, EventSequenceMutation active) =>
        Builders<EventSequenceMutationHeadEntry>.Filter.And(
            Builders<EventSequenceMutationHeadEntry>.Filter.Eq(_ => _.EventSequenceId, targetId),
            Builders<EventSequenceMutationHeadEntry>.Filter.Eq(_ => _.LastAssignedOrdinal, active.Ordinal),
            Builders<EventSequenceMutationHeadEntry>.Filter.Eq(_ => _.Active, active));

    async Task<EventSequenceMutationBeginResult?> ResolveArchivedPermanentRecord(
        EventSequenceKey scope,
        EventSequenceMutationRequest request,
        CancellationToken cancellationToken)
    {
        var document = await FindHistoryById(request.Id, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            return null;
        }

        var history = ToStorageHistory(document);

        EventSequenceMutationDefinition candidateDefinition;
        try
        {
            candidateDefinition = EventSequenceMutationDefinition.Create(scope, request, history.Target);
        }
        catch (InvalidEventSequenceMutation)
        {
            // The stored target could not reproduce a valid definition input alongside the caller's request and
            // this scope - the persisted history is malformed.
            return EventSequenceMutationBeginResult.Corrupt();
        }

        if (candidateDefinition.DefinitionDigestV1 != history.TerminalWitness.DefinitionDigestV1)
        {
            // The candidate, recomputed purely from the caller's own request plus the persisted target, does not
            // reproduce the digest that was witnessed at archive time - this is not a replay of the same request.
            return EventSequenceMutationBeginResult.DefinitionConflict(request.Id);
        }

        var registration = new EventSequenceMutationRegistration(
            candidateDefinition,
            EventSequenceMutationRegistryLifecycle.Archived,
            history.Ordinal,
            history.TerminalWitness);

        if (!EventSequenceMutationValidator.ValidateArchivedRegistration(scope, registration, history).IsValid)
        {
            return EventSequenceMutationBeginResult.Corrupt();
        }

        await ReleaseArchivedHead(scope, history, cancellationToken).ConfigureAwait(false);
        return EventSequenceMutationBeginResult.Archived(scope, registration, history);
    }

    async Task<EventSequenceMutationBeginResult> ReserveNewMutation(
        EventSequenceKey scope,
        EventSequenceMutationRequest request,
        EventSequenceMutationTarget proposedTarget,
        EventSequenceMutationHeadEntry? head,
        EventSequenceId targetId,
        CancellationToken cancellationToken)
    {
        var deterministicValidation = EventSequenceMutationValidator.ValidateDeterministicId(request);
        if (!deterministicValidation.IsValid)
        {
            return EventSequenceMutationBeginResult.Invalid(deterministicValidation);
        }

        var targetValidation = EventSequenceMutationValidator.ValidateTarget(proposedTarget);
        if (!targetValidation.IsValid)
        {
            return EventSequenceMutationBeginResult.Invalid(targetValidation);
        }

        var definitionValidation = EventSequenceMutationValidator.ValidateDefinitionInputs(scope, request, proposedTarget);
        if (!definitionValidation.IsValid)
        {
            return EventSequenceMutationBeginResult.Invalid(definitionValidation);
        }

        if (head is not null && !IsValidHead(scope, head.Coverage, head.LastAssignedOrdinal, head.Active))
        {
            return EventSequenceMutationBeginResult.Corrupt();
        }

        long nextOrdinalValue;
        try
        {
            nextOrdinalValue = checked((head?.LastAssignedOrdinal ?? EventSequenceMutationOrdinal.NotSet).Value + 1);
        }
        catch (OverflowException)
        {
            return EventSequenceMutationBeginResult.Corrupt();
        }

        var ordinal = new EventSequenceMutationOrdinal(nextOrdinalValue);
        var definition = EventSequenceMutationDefinition.Create(scope, request, proposedTarget);
        var active = new EventSequenceMutation(
            definition,
            ordinal,
            EventSequenceMutationStateVersion.First,
            EventSequenceMutationPhase.Reserved,
            EventSequenceMutationPhase.None,
            EventSequenceMutationRepairState.Unspecified);

        var filter = Builders<EventSequenceMutationHeadEntry>.Filter.And(
            Builders<EventSequenceMutationHeadEntry>.Filter.Eq(_ => _.EventSequenceId, targetId),
            Builders<EventSequenceMutationHeadEntry>.Filter.Eq(_ => _.Active, null),
            Builders<EventSequenceMutationHeadEntry>.Filter.Eq(_ => _.LastAssignedOrdinal, head?.LastAssignedOrdinal ?? EventSequenceMutationOrdinal.NotSet));

        var update = Builders<EventSequenceMutationHeadEntry>.Update
            .Set(_ => _.Active, active)
            .Set(_ => _.LastAssignedOrdinal, ordinal);

        var options = new FindOneAndUpdateOptions<EventSequenceMutationHeadEntry>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var updated = await TryFindOneAndUpdate(filter, update, options, cancellationToken).ConfigureAwait(false);
        if (updated is not null)
        {
            return EventSequenceMutationBeginResult.Reserved(active, EventSequenceMutationStateToken.Create(scope, active));
        }

        var current = await FindHead(targetId, cancellationToken).ConfigureAwait(false);
        if (current?.Active is null)
        {
            return EventSequenceMutationBeginResult.Contended();
        }

        return current.Active.Id == request.Id
            ? ResolveBoundResume(scope, request, current)
            : EventSequenceMutationBeginResult.MutationAlreadyInProgress(current.Active.Id);
    }

    async Task<EventSequenceMutationRegistryArchiveResult> RecoverArchive(
        EventSequenceKey scope,
        EventSequenceMutationStateToken token,
        EventSequenceMutationHistoryEntry document,
        CancellationToken cancellationToken)
    {
        var history = ToStorageHistory(document);
        var result = EventSequenceMutationRegistryArchiveResult.FromReceipt(scope, token, history);
        if (result.IsSuccess)
        {
            await ReleaseArchivedHead(scope, history, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    async Task ReleaseArchivedHead(EventSequenceKey scope, ProviderNeutralHistoryEntry history, CancellationToken cancellationToken)
    {
        var filter = Builders<EventSequenceMutationHeadEntry>.Filter;
        var exactTerminalHead = filter.And(
            filter.Eq(_ => _.EventSequenceId, scope.EventSequenceId),
            filter.Eq(_ => _.LastAssignedOrdinal, history.Ordinal),
            filter.Eq(_ => _.Active!.Definition.Request.Id, history.Id),
            filter.Eq(_ => _.Active!.Ordinal, history.Ordinal),
            filter.Eq(_ => _.Active!.Definition.DefinitionDigestV1, history.TerminalWitness.DefinitionDigestV1),
            filter.Eq(_ => _.Active!.StateVersion, new EventSequenceMutationStateVersion(history.TerminalWitness.FinalStateVersion.Value - 1)),
            filter.Eq(_ => _.Active!.Phase, EventSequenceMutationPhase.SourceCommitted),
            filter.Eq(_ => _.Active!.BlockedFrom, EventSequenceMutationPhase.None),
            filter.Eq(_ => _.Active!.RepairState, history.RepairState));
        await _heads.UpdateOneAsync(exactTerminalHead, Builders<EventSequenceMutationHeadEntry>.Update.Set(_ => _.Active, null), cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs an upsert-if-absent-or-matching <c>findAndModify</c>, treating a lost upsert race as a non-match
    /// rather than an unhandled exception.
    /// </summary>
    /// <remarks>
    /// When the filter fails to match (either because no document exists yet, or because an existing document's
    /// other fields no longer match), Mongo attempts to insert a new document built from the filter's equality
    /// terms. Two callers racing the exact same missing-document case can both reach that insert attempt before
    /// either commits; only one insert can satisfy the unique <c>_id</c> index, and the loser observes this as a
    /// duplicate-key error from the driver rather than a clean non-match. Swallowing it here and returning
    /// <see langword="null"/> lets every caller re-read the now-settled document through the same fallback path
    /// used for a genuine non-match.
    /// </remarks>
    /// <param name="filter">The <see cref="FilterDefinition{TDocument}"/> to match the head document with.</param>
    /// <param name="update">The <see cref="UpdateDefinition{TDocument}"/> to apply on a match or upsert-insert.</param>
    /// <param name="options">The <see cref="FindOneAndUpdateOptions{TDocument}"/> controlling the upsert.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>The updated document, or <see langword="null"/> when the filter did not match and the upsert lost a race.</returns>
    async Task<EventSequenceMutationHeadEntry?> TryFindOneAndUpdate(
        FilterDefinition<EventSequenceMutationHeadEntry> filter,
        UpdateDefinition<EventSequenceMutationHeadEntry> update,
        FindOneAndUpdateOptions<EventSequenceMutationHeadEntry> options,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _heads.FindOneAndUpdateAsync(filter, update, options, cancellationToken).ConfigureAwait(false);
        }
        catch (MongoCommandException ex) when (ex.Code == 11000 || ex.Message.Contains("E11000", StringComparison.Ordinal))
        {
            return null;
        }
    }

    async Task<EventSequenceMutationHeadEntry?> FindHead(EventSequenceId targetId, CancellationToken cancellationToken) =>
        await _heads.Find(Builders<EventSequenceMutationHeadEntry>.Filter.Eq(_ => _.EventSequenceId, targetId))
            .Limit(1)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    async Task<EventSequenceMutationHistoryEntry?> FindHistoryById(EventSequenceMutationId id, CancellationToken cancellationToken) =>
        await _history.Find(Builders<EventSequenceMutationHistoryEntry>.Filter.Eq(_ => _.MutationId, id))
            .Limit(1)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    EventSequenceKey Scope(EventSequenceMutationIdentity target) => new(target.Display, eventStore, @namespace);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations;

/// <summary>
/// Represents a namespace-scoped SQL event sequence mutation registry.
/// </summary>
/// <param name="eventStore">The event store that owns the registry.</param>
/// <param name="namespace">The namespace that owns the registry.</param>
/// <param name="database">The <see cref="IDatabase"/> used for storage operations.</param>
/// <remarks>
/// Every operation opens its own <see cref="NamespaceDbContext"/> scope and expresses fencing
/// through compare-and-swap <c>UPDATE ... WHERE</c> statements (<see cref="EntityFrameworkQueryableExtensions"/>
/// style <c>ExecuteUpdateAsync</c>) or unique-constraint-guarded inserts, rather than an in-process
/// lock - concurrent callers race the database itself, exactly as production traffic would.
/// <para>
/// A mutation's command payload is only persisted on the head row's <c>Active*</c> columns while the
/// mutation is bound (not yet archived); the history table is deliberately payload-free (see
/// <see cref="Cratis.Chronicle.Storage.EventSequences.Mutations.EventSequenceMutationHistoryEntry"/>).
/// <see cref="Archive"/> therefore clears only <see cref="EventSequenceMutationHeadEntry.ActiveMutationId"/>
/// when it releases a head row - not the remaining <c>Active*</c> columns - so that an immediate retry of
/// <see cref="Transition"/> or <see cref="Archive"/> against the same, still-archived mutation can still
/// reconstruct a digest-validating registration from the residual columns. A later <see cref="Begin"/>
/// against the same target overwrites every <c>Active*</c> column unconditionally, so the residue never
/// leaks into a new mutation. If a newer mutation has since reused the target sequence and overwritten the
/// residue before a stale retry arrives, that retry cannot be verified against the original payload (the
/// history table alone is not enough) and reports <see cref="EventSequenceMutationRegistryTransitionOutcome.StateConflict"/> /
/// <see cref="EventSequenceMutationRegistryArchiveOutcome.StateConflict"/> rather than fabricating success.
/// </para>
/// </remarks>
public sealed class EventSequenceMutationRegistry(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    IDatabase database) : IEventSequenceMutationRegistry
{
    /// <inheritdoc/>
    public Task<EventSequenceMutationBeginResult> Begin(
        EventSequenceMutationRequest request,
        EventSequenceMutationTarget proposedTarget,
        CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<EventSequenceMutationBeginResult>(cancellationToken)
            : BeginCore(request, proposedTarget, cancellationToken);

    /// <inheritdoc/>
    public Task<EventSequenceMutationRegistryTransitionResult> Transition(
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token,
        EventSequenceMutationTransition transition,
        CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<EventSequenceMutationRegistryTransitionResult>(cancellationToken)
            : TransitionCore(target, token, transition, cancellationToken);

    /// <inheritdoc/>
    public Task<EventSequenceMutationRegistryArchiveResult> Archive(
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token,
        CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<EventSequenceMutationRegistryArchiveResult>(cancellationToken)
            : ArchiveCore(target, token, cancellationToken);

    /// <inheritdoc/>
    public Task<EventSequenceMutationTrackingResult> BeginTracking(
        EventSequenceMutationIdentity target,
        EventSequenceMutationCoverage expected,
        CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<EventSequenceMutationTrackingResult>(cancellationToken)
            : BeginTrackingCore(target, expected, cancellationToken);

    static async Task<bool> TryInsertFreshHead(NamespaceDbContext db, EventSequenceId key, EventSequenceMutation active, CancellationToken cancellationToken)
    {
        var row = new EventSequenceMutationHeadEntry
        {
            EventSequenceId = key,
            Coverage = EventSequenceMutationCoverage.Untracked,
            LastAssignedOrdinal = active.Ordinal
        };
        EventSequenceMutationRowConversion.SetActiveColumns(row, active);
        db.EventSequenceMutationHeads.Add(row);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            db.Entry(row).State = EntityState.Detached;
            return false;
        }
    }

    static async Task<bool> TryClaimFreedHead(NamespaceDbContext db, EventSequenceId key, EventSequenceMutation active, CancellationToken cancellationToken)
    {
        var affected = await db.EventSequenceMutationHeads
            .Where(h => h.EventSequenceId == key && h.ActiveMutationId == null)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(h => h.LastAssignedOrdinal, active.Ordinal)
                    .SetProperty(h => h.ActiveMutationId, active.Id)
                    .SetProperty(h => h.ActiveOrdinal, active.Ordinal)
                    .SetProperty(h => h.ActiveStateVersion, active.StateVersion)
                    .SetProperty(h => h.ActiveOriginSequence, (EventSequenceId)active.Origin.Sequence.Display)
                    .SetProperty(h => h.ActiveOriginSequenceNumber, active.Origin.SequenceNumber)
                    .SetProperty(h => h.ActiveKind, active.Kind)
                    .SetProperty(h => h.ActiveCommandPayload, active.Command.Payload)
                    .SetProperty(h => h.ActiveCommandHash, active.Command.Hash)
                    .SetProperty(h => h.ActiveTargetStart, active.Target.Start)
                    .SetProperty(h => h.ActiveTargetEndExclusive, active.Target.EndExclusive)
                    .SetProperty(h => h.ActiveTargetExpectedCount, active.Target.ExpectedCount)
                    .SetProperty(h => h.ActiveDefinitionDigestV1, active.Definition.DefinitionDigestV1)
                    .SetProperty(h => h.ActivePhase, active.Phase)
                    .SetProperty(h => h.ActiveBlockedFrom, active.BlockedFrom)
                    .SetProperty(h => h.ActiveRepairState, active.RepairState),
                cancellationToken);
        return affected == 1;
    }

    static EventSequenceMutationBeginResult ResumeBound(EventSequenceKey scope, EventSequenceMutationRequest request, EventSequenceMutationHeadEntry row)
    {
        if (!EventSequenceMutationRowConversion.MatchesBoundRequest(row, request))
        {
            return EventSequenceMutationBeginResult.DefinitionConflict(request.Id);
        }

        var active = EventSequenceMutationRowConversion.ReconstructActive(row, request.TargetSequence);
        return EventSequenceMutationBeginResult.Resumed(active, EventSequenceMutationStateToken.Create(scope, active));
    }

    static EventSequenceMutationBeginResult ResumeArchived(
        EventSequenceKey scope,
        EventSequenceMutationRequest request,
        EventSequenceMutationHistoryEntry historyRow)
    {
        var frozenTarget = new EventSequenceMutationTarget(historyRow.TargetStart, historyRow.TargetEndExclusive, historyRow.TargetExpectedCount);
        var recomputedDigest = EventSequenceMutationDigestCalculator.CalculateDefinitionDigest(scope, request, frozenTarget);
        if (recomputedDigest != historyRow.DefinitionDigestV1)
        {
            return EventSequenceMutationBeginResult.DefinitionConflict(request.Id);
        }

        var definition = EventSequenceMutationDefinition.Create(scope, request, frozenTarget);
        var witness = EventSequenceMutationRowConversion.ToTerminalWitness(historyRow);
        var registration = new EventSequenceMutationRegistration(definition, EventSequenceMutationRegistryLifecycle.Archived, historyRow.Ordinal, witness);
        var history = EventSequenceMutationRowConversion.ToStorageHistory(historyRow);
        return EventSequenceMutationBeginResult.Archived(scope, registration, history);
    }

    static bool IsExactArchiveRetryToken(EventSequenceMutationStateToken token, EventSequenceMutationHistoryEntry historyRow) =>
        token.Phase == EventSequenceMutationPhase.SourceCommitted &&
        token.BlockedFrom == EventSequenceMutationPhase.None &&
        token.RepairState == historyRow.RepairState &&
        token.StateVersion.Value < long.MaxValue &&
        token.StateVersion.Value + 1 == historyRow.FinalStateVersion.Value;

    async Task<EventSequenceMutationBeginResult> BeginCore(
        EventSequenceMutationRequest request,
        EventSequenceMutationTarget proposedTarget,
        CancellationToken cancellationToken)
    {
        if (request is null || request.Id is null || request.Id == EventSequenceMutationId.NotSet ||
            request.TargetSequence?.Key.IsInitialized != true)
        {
            return EventSequenceMutationBeginResult.Invalid(EventSequenceMutationValidator.ValidateRequest(request));
        }

        await using var scope = await database.Namespace(eventStore, @namespace);
        var db = scope.DbContext;
        var key = (EventSequenceId)request.TargetSequence.Display;
        var mutationScope = Scope(request.TargetSequence);

        // A mutation identifier is permanently and globally bound once archived - check this first,
        // regardless of which target sequence's head row a fresh call would otherwise consult.
        var archivedRow = await db.EventSequenceMutationHistory.AsNoTracking()
            .FirstOrDefaultAsync(h => h.MutationId == request.Id, cancellationToken);
        if (archivedRow is not null)
        {
            return ResumeArchived(mutationScope, request, archivedRow);
        }

        var headRow = await db.EventSequenceMutationHeads.AsNoTracking()
            .FirstOrDefaultAsync(h => h.EventSequenceId == key, cancellationToken);

        if (headRow is not null && headRow.ActiveMutationId == request.Id)
        {
            return ResumeBound(mutationScope, request, headRow);
        }

        if (headRow?.ActiveMutationId is not null)
        {
            return EventSequenceMutationBeginResult.MutationAlreadyInProgress(headRow.ActiveMutationId);
        }

        var validation = EventSequenceMutationValidator.ValidateRequest(request);
        if (!validation.IsValid)
        {
            return EventSequenceMutationBeginResult.Invalid(validation);
        }

        validation = EventSequenceMutationValidator.ValidateDeterministicId(request);
        if (!validation.IsValid)
        {
            return EventSequenceMutationBeginResult.Invalid(validation);
        }

        validation = EventSequenceMutationValidator.ValidateTarget(proposedTarget);
        if (!validation.IsValid)
        {
            return EventSequenceMutationBeginResult.Invalid(validation);
        }

        validation = EventSequenceMutationValidator.ValidateDefinitionInputs(mutationScope, request, proposedTarget);
        if (!validation.IsValid)
        {
            return EventSequenceMutationBeginResult.Invalid(validation);
        }

        long ordinalValue;
        try
        {
            ordinalValue = checked((headRow?.LastAssignedOrdinal.Value ?? 0L) + 1);
        }
        catch (OverflowException)
        {
            return EventSequenceMutationBeginResult.Corrupt();
        }

        var definition = EventSequenceMutationDefinition.Create(mutationScope, request, proposedTarget);
        var ordinal = new EventSequenceMutationOrdinal(ordinalValue);
        var active = new EventSequenceMutation(
            definition,
            ordinal,
            EventSequenceMutationStateVersion.First,
            EventSequenceMutationPhase.Reserved,
            EventSequenceMutationPhase.None,
            EventSequenceMutationRepairState.Unspecified);

        var claimed = headRow is null
            ? await TryInsertFreshHead(db, key, active, cancellationToken)
            : await TryClaimFreedHead(db, key, active, cancellationToken);

        if (claimed)
        {
            return EventSequenceMutationBeginResult.Reserved(active, EventSequenceMutationStateToken.Create(mutationScope, active));
        }

        // Lost a race for this row - re-read and resolve exactly as the initial read would have.
        var raced = await db.EventSequenceMutationHeads.AsNoTracking()
            .FirstOrDefaultAsync(h => h.EventSequenceId == key, cancellationToken);
        if (raced?.ActiveMutationId == request.Id)
        {
            return ResumeBound(mutationScope, request, raced);
        }

        return raced?.ActiveMutationId is { } racedId
            ? EventSequenceMutationBeginResult.MutationAlreadyInProgress(racedId)
            : EventSequenceMutationBeginResult.Contended();
    }

    async Task<EventSequenceMutationRegistryTransitionResult> TransitionCore(
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token,
        EventSequenceMutationTransition transition,
        CancellationToken cancellationToken)
    {
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

        await using var scope = await database.Namespace(eventStore, @namespace);
        var db = scope.DbContext;
        var key = (EventSequenceId)target.Display;
        var mutationScope = Scope(target);

        var row = await db.EventSequenceMutationHeads.AsNoTracking()
            .FirstOrDefaultAsync(h => h.EventSequenceId == key, cancellationToken);

        if (row is null || row.ActiveMutationId != token.Id)
        {
            return await ResolveArchivedTransition(db, mutationScope, target, token, row, cancellationToken);
        }

        if (mutationScope != token.Scope || target.Key != token.TargetKey)
        {
            return EventSequenceMutationRegistryTransitionResult.StateConflict();
        }

        var current = EventSequenceMutationRowConversion.ReconstructActive(row, target);
        if (current.Ordinal != token.Ordinal || current.Definition.DefinitionDigestV1 != token.DefinitionDigestV1)
        {
            return EventSequenceMutationRegistryTransitionResult.StateConflict();
        }

        var result = EventSequenceMutationStateMachine.Apply(mutationScope, current, transition, token);
        if (!result.Validation.IsValid)
        {
            return EventSequenceMutationRegistryTransitionResult.Invalid(result.Validation);
        }

        return result.Outcome switch
        {
            EventSequenceMutationTransitionOutcome.Conflict => EventSequenceMutationRegistryTransitionResult.StateConflict(),
            EventSequenceMutationTransitionOutcome.AlreadyApplied => EventSequenceMutationRegistryTransitionResult.AlreadyApplied(result.Mutation!, result.Token!),
            EventSequenceMutationTransitionOutcome.Applied => await WriteTransition(db, key, current, result, cancellationToken),
            _ => EventSequenceMutationRegistryTransitionResult.Corrupt()
        };
    }

    async Task<EventSequenceMutationRegistryTransitionResult> WriteTransition(
        NamespaceDbContext db,
        EventSequenceId key,
        EventSequenceMutation current,
        EventSequenceMutationTransitionResult result,
        CancellationToken cancellationToken)
    {
        var successor = result.Mutation!;
        var affected = await db.EventSequenceMutationHeads
            .Where(h => h.EventSequenceId == key && h.ActiveStateVersion == current.StateVersion)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(h => h.ActiveStateVersion, successor.StateVersion)
                    .SetProperty(h => h.ActivePhase, successor.Phase)
                    .SetProperty(h => h.ActiveBlockedFrom, successor.BlockedFrom)
                    .SetProperty(h => h.ActiveRepairState, successor.RepairState),
                cancellationToken);

        return affected == 1
            ? EventSequenceMutationRegistryTransitionResult.Applied(successor, result.Token!)
            : EventSequenceMutationRegistryTransitionResult.StateConflict();
    }

    async Task<EventSequenceMutationRegistryTransitionResult> ResolveArchivedTransition(
        NamespaceDbContext db,
        EventSequenceKey scope,
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token,
        EventSequenceMutationHeadEntry? row,
        CancellationToken cancellationToken)
    {
        var historyRow = await db.EventSequenceMutationHistory.AsNoTracking()
            .FirstOrDefaultAsync(h => h.MutationId == token.Id, cancellationToken);
        if (historyRow is null)
        {
            return EventSequenceMutationRegistryTransitionResult.StateConflict();
        }

        if (scope != token.Scope || target.Key != token.TargetKey ||
            historyRow.Ordinal != token.Ordinal || historyRow.DefinitionDigestV1 != token.DefinitionDigestV1)
        {
            return EventSequenceMutationRegistryTransitionResult.StateConflict();
        }

        var reconstructed = EventSequenceMutationRowConversion.TryReconstructArchivedFromResidue(target, row, historyRow);
        return reconstructed is { } archived
            ? EventSequenceMutationRegistryTransitionResult.AlreadyArchived(scope, archived.Registration, archived.History)
            : EventSequenceMutationRegistryTransitionResult.StateConflict();
    }

    async Task<EventSequenceMutationRegistryArchiveResult> ArchiveCore(
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token,
        CancellationToken cancellationToken)
    {
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

        await using var scope = await database.Namespace(eventStore, @namespace);
        var db = scope.DbContext;
        var key = (EventSequenceId)target.Display;
        var mutationScope = Scope(target);

        var row = await db.EventSequenceMutationHeads.AsNoTracking()
            .FirstOrDefaultAsync(h => h.EventSequenceId == key, cancellationToken);

        if (row is null || row.ActiveMutationId != token.Id)
        {
            return await ResolveArchivedArchive(db, mutationScope, target, token, row, cancellationToken);
        }

        if (mutationScope != token.Scope || target.Key != token.TargetKey)
        {
            return EventSequenceMutationRegistryArchiveResult.StateConflict();
        }

        var current = EventSequenceMutationRowConversion.ReconstructActive(row, target);
        if (current.Ordinal != token.Ordinal || current.Definition.DefinitionDigestV1 != token.DefinitionDigestV1)
        {
            return EventSequenceMutationRegistryArchiveResult.StateConflict();
        }

        var prepared = EventSequenceMutationStateMachine.PrepareArchive(mutationScope, current, token);
        if (!prepared.Validation.IsValid)
        {
            return EventSequenceMutationRegistryArchiveResult.Invalid(prepared.Validation);
        }

        return prepared.Outcome switch
        {
            EventSequenceMutationArchiveOutcome.Conflict => EventSequenceMutationRegistryArchiveResult.StateConflict(),
            EventSequenceMutationArchiveOutcome.Prepared when prepared.History is not null =>
                await CommitArchive(db, mutationScope, key, current, prepared.History, cancellationToken),
            _ => EventSequenceMutationRegistryArchiveResult.Corrupt()
        };
    }

    async Task<EventSequenceMutationRegistryArchiveResult> CommitArchive(
        NamespaceDbContext db,
        EventSequenceKey scope,
        EventSequenceId key,
        EventSequenceMutation preArchive,
        Chronicle.Storage.EventSequences.Mutations.EventSequenceMutationHistoryEntry history,
        CancellationToken cancellationToken)
    {
        var registration = new EventSequenceMutationRegistration(
            preArchive.Definition,
            EventSequenceMutationRegistryLifecycle.Archived,
            history.Ordinal,
            history.TerminalWitness);

        var historyRow = EventSequenceMutationRowConversion.ToRow(key, history);
        db.EventSequenceMutationHistory.Add(historyRow);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.Entry(historyRow).State = EntityState.Detached;

            // Someone else already archived this mutation id (the unique index on MutationId).
            // Verify it is the exact lineage we just prepared before reporting success.
            var existing = await db.EventSequenceMutationHistory.AsNoTracking()
                .FirstOrDefaultAsync(h => h.MutationId == history.Id, cancellationToken);
            if (existing is null)
            {
                return EventSequenceMutationRegistryArchiveResult.Corrupt();
            }

            var existingHistory = EventSequenceMutationRowConversion.ToStorageHistory(existing);
            return existingHistory == history
                ? EventSequenceMutationRegistryArchiveResult.AlreadyArchived(scope, registration, existingHistory)
                : EventSequenceMutationRegistryArchiveResult.StateConflict();
        }

        // Only the fencing column is cleared here - see the type-level remarks for why the
        // remaining Active* columns are intentionally left in place until the next Begin.
        var clearedRows = await db.EventSequenceMutationHeads
            .Where(h => h.EventSequenceId == key && h.ActiveStateVersion == preArchive.StateVersion)
            .ExecuteUpdateAsync(s => s.SetProperty(h => h.ActiveMutationId, (EventSequenceMutationId?)null), cancellationToken);

        return clearedRows == 1
            ? EventSequenceMutationRegistryArchiveResult.Archived(scope, registration, history)
            : EventSequenceMutationRegistryArchiveResult.Corrupt();
    }

    async Task<EventSequenceMutationRegistryArchiveResult> ResolveArchivedArchive(
        NamespaceDbContext db,
        EventSequenceKey scope,
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token,
        EventSequenceMutationHeadEntry? row,
        CancellationToken cancellationToken)
    {
        var historyRow = await db.EventSequenceMutationHistory.AsNoTracking()
            .FirstOrDefaultAsync(h => h.MutationId == token.Id, cancellationToken);
        if (historyRow is null)
        {
            return EventSequenceMutationRegistryArchiveResult.StateConflict();
        }

        if (scope != token.Scope || target.Key != token.TargetKey ||
            historyRow.Ordinal != token.Ordinal || historyRow.DefinitionDigestV1 != token.DefinitionDigestV1)
        {
            return EventSequenceMutationRegistryArchiveResult.StateConflict();
        }

        if (!IsExactArchiveRetryToken(token, historyRow))
        {
            return EventSequenceMutationRegistryArchiveResult.StateConflict();
        }

        var reconstructed = EventSequenceMutationRowConversion.TryReconstructArchivedFromResidue(target, row, historyRow);
        return reconstructed is { } archived
            ? EventSequenceMutationRegistryArchiveResult.AlreadyArchived(scope, archived.Registration, archived.History)
            : EventSequenceMutationRegistryArchiveResult.StateConflict();
    }

    async Task<EventSequenceMutationTrackingResult> BeginTrackingCore(
        EventSequenceMutationIdentity target,
        EventSequenceMutationCoverage expected,
        CancellationToken cancellationToken)
    {
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

        var mutationScope = Scope(target);
        validation = EventSequenceMutationValidator.ValidateScope(mutationScope);
        if (!validation.IsValid)
        {
            return EventSequenceMutationTrackingResult.Invalid(validation);
        }

        await using var scope = await database.Namespace(eventStore, @namespace);
        var db = scope.DbContext;
        var key = (EventSequenceId)target.Display;

        var row = await db.EventSequenceMutationHeads.AsNoTracking()
            .FirstOrDefaultAsync(h => h.EventSequenceId == key, cancellationToken);

        if (row is null)
        {
            var fresh = new EventSequenceMutationHeadEntry
            {
                EventSequenceId = key,
                Coverage = EventSequenceMutationCoverage.Unsealed,
                LastAssignedOrdinal = EventSequenceMutationOrdinal.NotSet
            };
            db.EventSequenceMutationHeads.Add(fresh);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return EventSequenceMutationTrackingResult.Began();
            }
            catch (DbUpdateException)
            {
                db.Entry(fresh).State = EntityState.Detached;
                row = await db.EventSequenceMutationHeads.AsNoTracking()
                    .FirstOrDefaultAsync(h => h.EventSequenceId == key, cancellationToken);
                if (row is null)
                {
                    return EventSequenceMutationTrackingResult.Indeterminate();
                }
            }
        }

        return row.Coverage switch
        {
            EventSequenceMutationCoverage.Unsealed => EventSequenceMutationTrackingResult.AlreadyTracking(),
            EventSequenceMutationCoverage.Sealed => EventSequenceMutationTrackingResult.Conflict(EventSequenceMutationCoverage.Sealed),
            EventSequenceMutationCoverage.Untracked => await ClaimUntracked(db, key, cancellationToken),
            _ => EventSequenceMutationTrackingResult.Corrupt()
        };
    }

    async Task<EventSequenceMutationTrackingResult> ClaimUntracked(NamespaceDbContext db, EventSequenceId key, CancellationToken cancellationToken)
    {
        var affected = await db.EventSequenceMutationHeads
            .Where(h => h.EventSequenceId == key && h.Coverage == EventSequenceMutationCoverage.Untracked)
            .ExecuteUpdateAsync(s => s.SetProperty(h => h.Coverage, EventSequenceMutationCoverage.Unsealed), cancellationToken);
        if (affected == 1)
        {
            return EventSequenceMutationTrackingResult.Began();
        }

        var reread = await db.EventSequenceMutationHeads.AsNoTracking()
            .FirstOrDefaultAsync(h => h.EventSequenceId == key, cancellationToken);
        return reread?.Coverage switch
        {
            EventSequenceMutationCoverage.Unsealed => EventSequenceMutationTrackingResult.AlreadyTracking(),
            EventSequenceMutationCoverage.Sealed => EventSequenceMutationTrackingResult.Conflict(EventSequenceMutationCoverage.Sealed),
            _ => EventSequenceMutationTrackingResult.Indeterminate()
        };
    }

    EventSequenceKey Scope(EventSequenceMutationIdentity target) => new(target.Display, eventStore, @namespace);
}

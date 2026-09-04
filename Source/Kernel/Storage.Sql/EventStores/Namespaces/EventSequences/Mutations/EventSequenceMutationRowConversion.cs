// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations;

/// <summary>
/// Converts between the flat SQL persistence entities and the provider-neutral mutation domain types.
/// </summary>
static class EventSequenceMutationRowConversion
{
    /// <summary>
    /// Reconstructs the active mutation from a head row whose <see cref="EventSequenceMutationHeadEntry.ActiveMutationId"/> is set.
    /// </summary>
    /// <param name="row">The head row to reconstruct from.</param>
    /// <param name="target">The target identity the row belongs to.</param>
    /// <returns>The reconstructed active mutation.</returns>
    internal static EventSequenceMutation ReconstructActive(EventSequenceMutationHeadEntry row, EventSequenceMutationIdentity target)
    {
        var definition = ReconstructDefinitionFromResidue(row, target, row.ActiveMutationId!);
        return new EventSequenceMutation(
            definition,
            row.ActiveOrdinal!,
            row.ActiveStateVersion!,
            row.ActivePhase!.Value,
            row.ActiveBlockedFrom!.Value,
            row.ActiveRepairState!.Value);
    }

    /// <summary>
    /// Reconstructs a mutation definition from a head row's <c>Active*</c> columns, using an explicit
    /// mutation identifier rather than the row's own <see cref="EventSequenceMutationHeadEntry.ActiveMutationId"/>.
    /// </summary>
    /// <param name="row">The head row to reconstruct from - only its non-identifier <c>Active*</c> columns are read.</param>
    /// <param name="target">The target identity the row belongs to.</param>
    /// <param name="mutationId">The mutation identifier to reconstruct the definition for.</param>
    /// <returns>The reconstructed definition.</returns>
    /// <remarks>
    /// Used to reconstruct a definition from the residual <c>Active*</c> columns a head row keeps after
    /// <see cref="EventSequenceMutationRegistry.Archive"/> clears only <see cref="EventSequenceMutationHeadEntry.ActiveMutationId"/> -
    /// see the registry's type-level remarks for why the identifier itself cannot be read from the row in that case.
    /// </remarks>
    internal static EventSequenceMutationDefinition ReconstructDefinitionFromResidue(
        EventSequenceMutationHeadEntry row,
        EventSequenceMutationIdentity target,
        EventSequenceMutationId mutationId)
    {
        var origin = new EventSequenceMutationOrigin(
            RecreateIdentity(row.ActiveOriginSequence!.Value),
            new EventSequenceNumber(row.ActiveOriginSequenceNumber!.Value));
        var request = new EventSequenceMutationRequest(
            mutationId,
            target,
            origin,
            row.ActiveKind!.Value,
            new EventSequenceMutationCommandEnvelope(row.ActiveCommandPayload!, row.ActiveCommandHash!));
        var frozenTarget = new EventSequenceMutationTarget(row.ActiveTargetStart!, row.ActiveTargetEndExclusive!, row.ActiveTargetExpectedCount!);
        return new EventSequenceMutationDefinition(request, frozenTarget, row.ActiveDefinitionDigestV1!);
    }

    /// <summary>
    /// Sets every <c>Active*</c> column on a head row from an active mutation.
    /// </summary>
    /// <param name="row">The head row to update.</param>
    /// <param name="active">The active mutation to write.</param>
    internal static void SetActiveColumns(EventSequenceMutationHeadEntry row, EventSequenceMutation active)
    {
        row.ActiveMutationId = active.Id;
        row.ActiveOrdinal = active.Ordinal;
        row.ActiveStateVersion = active.StateVersion;
        row.ActiveOriginSequence = (Concepts.EventSequences.EventSequenceId)active.Origin.Sequence.Display;
        row.ActiveOriginSequenceNumber = active.Origin.SequenceNumber;
        row.ActiveKind = active.Kind;
        row.ActiveCommandPayload = active.Command.Payload;
        row.ActiveCommandHash = active.Command.Hash;
        row.ActiveTargetStart = active.Target.Start;
        row.ActiveTargetEndExclusive = active.Target.EndExclusive;
        row.ActiveTargetExpectedCount = active.Target.ExpectedCount;
        row.ActiveDefinitionDigestV1 = active.Definition.DefinitionDigestV1;
        row.ActivePhase = active.Phase;
        row.ActiveBlockedFrom = active.BlockedFrom;
        row.ActiveRepairState = active.RepairState;
    }

    /// <summary>
    /// Determines whether a bound (still payload-bearing) head row was registered from the exact request supplied.
    /// </summary>
    /// <param name="row">The head row currently bound to the request's mutation identifier.</param>
    /// <param name="request">The request to compare. A newly proposed target is intentionally not part of this comparison.</param>
    /// <returns><see langword="true"/> when every comparable field is exactly equal.</returns>
    internal static bool MatchesBoundRequest(EventSequenceMutationHeadEntry row, EventSequenceMutationRequest? request) =>
        request is { Origin.Sequence: not null, Origin.SequenceNumber: not null, Command.Payload: not null, Command.Hash: not null } &&
        string.Equals(row.ActiveOriginSequence!.Value, request.Origin.Sequence.Display, StringComparison.Ordinal) &&
        row.ActiveOriginSequenceNumber!.Value == request.Origin.SequenceNumber.Value &&
        row.ActiveKind!.Value == request.Kind &&
        string.Equals(row.ActiveCommandPayload, request.Command.Payload, StringComparison.Ordinal) &&
        string.Equals(row.ActiveCommandHash!.Value, request.Command.Hash.Value, StringComparison.Ordinal);

    /// <summary>
    /// Tries to reconstruct a validating archived registration from a history row plus the residual
    /// <c>Active*</c> columns a head row keeps after an archive that has not yet been reclaimed by a
    /// newer <see cref="EventSequenceMutationRegistry.Begin"/> call.
    /// </summary>
    /// <param name="target">The target identity the rows belong to.</param>
    /// <param name="row">The head row, or <see langword="null"/> when no row exists for the target.</param>
    /// <param name="historyRow">The permanent history row for the archived mutation.</param>
    /// <returns>The reconstructed registration and storage history entry, or <see langword="null"/> when reconstruction is not possible.</returns>
    internal static (EventSequenceMutationRegistration Registration, Chronicle.Storage.EventSequences.Mutations.EventSequenceMutationHistoryEntry History)? TryReconstructArchivedFromResidue(
        EventSequenceMutationIdentity target,
        EventSequenceMutationHeadEntry? row,
        EventSequenceMutationHistoryEntry historyRow)
    {
        if (row is null || row.ActiveMutationId is not null ||
            row.ActiveCommandHash is null || !string.Equals(row.ActiveCommandHash.Value, historyRow.CommandHash.Value, StringComparison.Ordinal) ||
            row.ActiveDefinitionDigestV1 is null || row.ActiveDefinitionDigestV1 != historyRow.DefinitionDigestV1)
        {
            return null;
        }

        var definition = ReconstructDefinitionFromResidue(row, target, historyRow.MutationId);
        var witness = ToTerminalWitness(historyRow);
        var registration = new EventSequenceMutationRegistration(definition, EventSequenceMutationRegistryLifecycle.Archived, historyRow.Ordinal, witness);
        var history = ToStorageHistory(historyRow);
        return (registration, history);
    }

    /// <summary>
    /// Builds the storage-layer terminal history entry from its persisted SQL row.
    /// </summary>
    /// <param name="row">The history row to convert.</param>
    /// <returns>The reconstructed terminal history entry.</returns>
    internal static Chronicle.Storage.EventSequences.Mutations.EventSequenceMutationHistoryEntry ToStorageHistory(EventSequenceMutationHistoryEntry row)
    {
        var witness = ToTerminalWitness(row);
        return new(
            row.MutationId,
            row.Ordinal,
            new EventSequenceMutationOrigin(RecreateIdentity(row.OriginSequence.Value), row.OriginSequenceNumber),
            row.Kind,
            row.CommandHash,
            new EventSequenceMutationTarget(row.TargetStart, row.TargetEndExclusive, row.TargetExpectedCount),
            row.RepairState,
            witness);
    }

    /// <summary>
    /// Builds the terminal witness carried by a persisted history row.
    /// </summary>
    /// <param name="row">The history row to convert.</param>
    /// <returns>The reconstructed terminal witness.</returns>
    internal static EventSequenceMutationTerminalWitness ToTerminalWitness(EventSequenceMutationHistoryEntry row) =>
        new(row.FinalStateVersion, row.DefinitionDigestV1, row.ReceiptDigestV1);

    /// <summary>
    /// Builds the SQL history row to insert for a prepared archive.
    /// </summary>
    /// <param name="eventSequenceId">The row key of the owning event sequence.</param>
    /// <param name="history">The prepared, provider-neutral history entry.</param>
    /// <returns>The SQL persistence entity to insert.</returns>
    internal static EventSequenceMutationHistoryEntry ToRow(Concepts.EventSequences.EventSequenceId eventSequenceId, Chronicle.Storage.EventSequences.Mutations.EventSequenceMutationHistoryEntry history) =>
        new()
        {
            EventSequenceId = eventSequenceId,
            Ordinal = history.Ordinal,
            MutationId = history.Id,
            OriginSequence = (Concepts.EventSequences.EventSequenceId)history.Origin.Sequence.Display,
            OriginSequenceNumber = history.Origin.SequenceNumber,
            Kind = history.Kind,
            CommandHash = history.CommandHash,
            TargetStart = history.Target.Start,
            TargetEndExclusive = history.Target.EndExclusive,
            TargetExpectedCount = history.Target.ExpectedCount,
            RepairState = history.RepairState,
            FinalStateVersion = history.TerminalWitness.FinalStateVersion,
            DefinitionDigestV1 = history.TerminalWitness.DefinitionDigestV1,
            ReceiptDigestV1 = history.TerminalWitness.ReceiptDigestV1
        };

    static EventSequenceMutationIdentity RecreateIdentity(string display) => EventSequenceMutationIdentity.TryCreate(display).Identity!;
}

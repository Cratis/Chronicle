// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Verifies retry bindings against payload-free durable mutation receipts.
/// </summary>
public static class EventSequenceMutationReceipt
{
    /// <summary>
    /// Determines whether a token belongs to a verified terminal receipt in the supplied scope.
    /// </summary>
    /// <param name="scope">The requested scope.</param>
    /// <param name="token">The retry token.</param>
    /// <param name="history">The durable receipt.</param>
    /// <returns>Whether the binding and receipt are valid.</returns>
    public static bool Matches(EventSequenceKey scope, EventSequenceMutationStateToken token, EventSequenceMutationHistoryEntry history) =>
        EventSequenceMutationValidator.ValidateToken(token).IsValid &&
        EventSequenceMutationValidator.ValidateHistory(scope, history).IsValid &&
        token.Scope == scope && token.Id == history.Id && token.Ordinal == history.Ordinal &&
        token.DefinitionDigestV1 == history.TerminalWitness.DefinitionDigestV1 &&
        token.StateVersion.Value < history.TerminalWitness.FinalStateVersion.Value;

    /// <summary>
    /// Determines whether a token is the exact predecessor of the durable archive.
    /// </summary>
    /// <param name="scope">The requested scope.</param>
    /// <param name="token">The retry token.</param>
    /// <param name="history">The durable receipt.</param>
    /// <returns>Whether this is an exact archive retry.</returns>
    public static bool IsExactArchiveRetry(EventSequenceKey scope, EventSequenceMutationStateToken token, EventSequenceMutationHistoryEntry history) =>
        Matches(scope, token, history) &&
        token.Phase == EventSequenceMutationPhase.SourceCommitted &&
        token.BlockedFrom == EventSequenceMutationPhase.None &&
        token.RepairState == history.RepairState &&
        token.StateVersion.Value + 1 == history.TerminalWitness.FinalStateVersion.Value;
}

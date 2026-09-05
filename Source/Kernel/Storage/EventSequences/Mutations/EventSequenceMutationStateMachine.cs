// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Applies the closed, provider-neutral event sequence mutation state graph.
/// </summary>
public static class EventSequenceMutationStateMachine
{
    /// <summary>
    /// Applies a transition fenced by a predecessor state token.
    /// </summary>
    /// <param name="scope">The event sequence scope.</param>
    /// <param name="current">The currently observed active mutation.</param>
    /// <param name="transition">The transition to apply.</param>
    /// <param name="predecessor">The expected predecessor token.</param>
    /// <returns>A closed transition result.</returns>
    public static EventSequenceMutationTransitionResult Apply(
        EventSequenceKey scope,
        EventSequenceMutation current,
        EventSequenceMutationTransition transition,
        EventSequenceMutationStateToken predecessor)
    {
        if (!Enum.IsDefined(transition) || transition == EventSequenceMutationTransition.Unspecified)
        {
            return Invalid(EventSequenceMutationValidationError.InvalidEnum, nameof(transition));
        }

        var validation = EventSequenceMutationValidator.ValidateActive(scope, current);
        if (!validation.IsValid)
        {
            return Invalid(validation);
        }

        validation = EventSequenceMutationValidator.ValidateToken(predecessor);
        if (!validation.IsValid)
        {
            return Invalid(validation);
        }

        if (Matches(scope, current, predecessor))
        {
            if (!TryGetDestination(current, transition, out var destination))
            {
                return Conflict(current);
            }

            if (current.StateVersion.Value == long.MaxValue)
            {
                return Invalid(EventSequenceMutationValidationError.StateVersionExhausted, nameof(current.StateVersion));
            }

            var successor = destination! with { StateVersion = current.StateVersion.Next() };
            return new(
                EventSequenceMutationTransitionOutcome.Applied,
                successor,
                EventSequenceMutationStateToken.Create(scope, successor),
                EventSequenceMutationValidationResult.Valid);
        }

        if (!HasSameBinding(scope, current, predecessor) || predecessor.StateVersion.Value == long.MaxValue || current.StateVersion.Value != predecessor.StateVersion.Value + 1)
        {
            return Conflict(current);
        }

        var observedPredecessor = current with
        {
            StateVersion = predecessor.StateVersion,
            Phase = predecessor.Phase,
            BlockedFrom = predecessor.BlockedFrom,
            RepairState = predecessor.RepairState
        };

        if (!TryGetDestination(observedPredecessor, transition, out var expectedDestination))
        {
            return Conflict(current);
        }

        var expected = expectedDestination! with { StateVersion = current.StateVersion };
        return current == expected
            ? new(
                EventSequenceMutationTransitionOutcome.AlreadyApplied,
                current,
                EventSequenceMutationStateToken.Create(scope, current),
                EventSequenceMutationValidationResult.Valid)
            : Conflict(current);
    }

    /// <summary>
    /// Prepares the immutable terminal receipt for an eligible active mutation without changing the active state.
    /// </summary>
    /// <param name="scope">The event sequence scope.</param>
    /// <param name="current">The currently observed active mutation.</param>
    /// <param name="token">The exact terminal state token.</param>
    /// <returns>The archive preparation result.</returns>
    public static EventSequenceMutationArchiveResult PrepareArchive(
        EventSequenceKey scope,
        EventSequenceMutation current,
        EventSequenceMutationStateToken token)
    {
        var validation = EventSequenceMutationValidator.ValidateActive(scope, current);
        if (!validation.IsValid)
        {
            return InvalidArchive(validation);
        }

        validation = EventSequenceMutationValidator.ValidateToken(token);
        if (!validation.IsValid)
        {
            return InvalidArchive(validation);
        }

        if (!Matches(scope, current, token) ||
            current.Phase != EventSequenceMutationPhase.SourceCommitted ||
            current.BlockedFrom != EventSequenceMutationPhase.None ||
            current.RepairState is not (EventSequenceMutationRepairState.NotRequired or EventSequenceMutationRepairState.Accepted or EventSequenceMutationRepairState.Unknown))
        {
            return new(EventSequenceMutationArchiveOutcome.Conflict, null, EventSequenceMutationValidationResult.Valid);
        }

        if (current.StateVersion.Value == long.MaxValue)
        {
            return InvalidArchive(new(EventSequenceMutationValidationError.StateVersionExhausted, nameof(current.StateVersion)));
        }

        var finalStateVersion = current.StateVersion.Next();
        var definitionDigest = current.Definition.DefinitionDigestV1;
        var provisionalWitness = new EventSequenceMutationTerminalWitness(
            finalStateVersion,
            definitionDigest,
            new(new byte[32]));
        var history = new EventSequenceMutationHistoryEntry(
            current.Id,
            current.Ordinal,
            current.Origin,
            current.Kind,
            current.Command.Hash,
            current.Target,
            current.RepairState,
            provisionalWitness);
        var receiptDigest = EventSequenceMutationDigestCalculator.CalculateReceiptDigest(
            scope,
            history,
            finalStateVersion,
            definitionDigest);
        history = history with
        {
            TerminalWitness = provisionalWitness with { ReceiptDigestV1 = receiptDigest }
        };

        return new(EventSequenceMutationArchiveOutcome.Prepared, history, EventSequenceMutationValidationResult.Valid);
    }

    static bool TryGetDestination(EventSequenceMutation source, EventSequenceMutationTransition transition, out EventSequenceMutation? destination)
    {
        destination = (source.Phase, source.BlockedFrom, source.RepairState, transition) switch
        {
            (EventSequenceMutationPhase.Reserved, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Unspecified, EventSequenceMutationTransition.BeginApplying) => source with { Phase = EventSequenceMutationPhase.Applying },
            (EventSequenceMutationPhase.Applying, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Unspecified, EventSequenceMutationTransition.BeginVerifying) => source with { Phase = EventSequenceMutationPhase.Verifying },
            (EventSequenceMutationPhase.Applying or EventSequenceMutationPhase.Verifying, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Unspecified, EventSequenceMutationTransition.Block) => source with { Phase = EventSequenceMutationPhase.Blocked, BlockedFrom = source.Phase },
            (EventSequenceMutationPhase.Blocked, EventSequenceMutationPhase.Applying or EventSequenceMutationPhase.Verifying, EventSequenceMutationRepairState.Unspecified, EventSequenceMutationTransition.Resume) => source with { Phase = source.BlockedFrom, BlockedFrom = EventSequenceMutationPhase.None },
            (EventSequenceMutationPhase.Verifying, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Unspecified, EventSequenceMutationTransition.CommitSourceWithoutRepair) => source with { Phase = EventSequenceMutationPhase.SourceCommitted, RepairState = EventSequenceMutationRepairState.NotRequired },
            (EventSequenceMutationPhase.Verifying, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Unspecified, EventSequenceMutationTransition.CommitSourceWithRepair) => source with { Phase = EventSequenceMutationPhase.SourceCommitted, RepairState = EventSequenceMutationRepairState.Pending },
            (EventSequenceMutationPhase.SourceCommitted, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Pending, EventSequenceMutationTransition.BeginRepairDispatch) => source with { RepairState = EventSequenceMutationRepairState.Dispatching },
            (EventSequenceMutationPhase.SourceCommitted, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Dispatching, EventSequenceMutationTransition.AcceptRepair) => source with { RepairState = EventSequenceMutationRepairState.Accepted },
            (EventSequenceMutationPhase.SourceCommitted, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Dispatching, EventSequenceMutationTransition.MarkRepairUnknown) => source with { RepairState = EventSequenceMutationRepairState.Unknown },
            _ => null
        };

        return destination is not null;
    }

    static bool Matches(EventSequenceKey scope, EventSequenceMutation current, EventSequenceMutationStateToken token) =>
        HasSameBinding(scope, current, token) &&
        current.StateVersion == token.StateVersion &&
        current.Phase == token.Phase &&
        current.BlockedFrom == token.BlockedFrom &&
        current.RepairState == token.RepairState;

    static bool HasSameBinding(EventSequenceKey scope, EventSequenceMutation current, EventSequenceMutationStateToken token) =>
        scope == token.Scope &&
        current.TargetSequence.Key == token.TargetKey &&
        current.Id == token.Id &&
        current.Ordinal == token.Ordinal &&
        current.Definition.DefinitionDigestV1 == token.DefinitionDigestV1;

    static EventSequenceMutationArchiveResult InvalidArchive(EventSequenceMutationValidationResult validation) =>
        new(EventSequenceMutationArchiveOutcome.Invalid, null, validation);

    static EventSequenceMutationTransitionResult Conflict(EventSequenceMutation current) =>
        new(EventSequenceMutationTransitionOutcome.Conflict, current, null, EventSequenceMutationValidationResult.Valid);

    static EventSequenceMutationTransitionResult Invalid(EventSequenceMutationValidationError error, string field) => Invalid(new(error, field));

    static EventSequenceMutationTransitionResult Invalid(EventSequenceMutationValidationResult validation) =>
        new(EventSequenceMutationTransitionOutcome.Invalid, null, null, validation);
}

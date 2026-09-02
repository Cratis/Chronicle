// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents the closed result of beginning an event sequence mutation.
/// </summary>
public sealed class EventSequenceMutationBeginResult
{
    EventSequenceMutationBeginResult(
        EventSequenceMutationBeginOutcome outcome,
        EventSequenceMutation? active,
        EventSequenceMutationStateToken? token,
        EventSequenceMutationHistoryEntry? history,
        EventSequenceMutationId? conflictingMutationId,
        EventSequenceMutationRegistryError error,
        EventSequenceMutationValidationResult? validation)
    {
        Validate(outcome, active, token, history, conflictingMutationId, error, validation);
        Outcome = outcome;
        Active = active;
        Token = token;
        History = history;
        ConflictingMutationId = conflictingMutationId;
        Error = error;
        Validation = validation;
    }

    /// <summary>Gets the closed begin outcome.</summary>
    public EventSequenceMutationBeginOutcome Outcome { get; }

    /// <summary>Gets the active mutation for an active success.</summary>
    public EventSequenceMutation? Active { get; }

    /// <summary>Gets the complete state token for an active success.</summary>
    public EventSequenceMutationStateToken? Token { get; }

    /// <summary>Gets the verified payload-free terminal history for an archived success.</summary>
    public EventSequenceMutationHistoryEntry? History { get; }

    /// <summary>Gets the non-sensitive mutation identity associated with a conflict.</summary>
    public EventSequenceMutationId? ConflictingMutationId { get; }

    /// <summary>Gets the non-sensitive typed error for a failed outcome.</summary>
    public EventSequenceMutationRegistryError Error { get; }

    /// <summary>Gets validation details for an invalid outcome.</summary>
    public EventSequenceMutationValidationResult? Validation { get; }

    /// <summary>Gets whether the result contains the exact mandatory success payload.</summary>
    public bool IsSuccess =>
        Outcome switch
        {
            EventSequenceMutationBeginOutcome.Reserved or
            EventSequenceMutationBeginOutcome.Resumed or
            EventSequenceMutationBeginOutcome.RecoveredReservation =>
                Active is not null && Token is not null && History is null && ConflictingMutationId is null &&
                Error == EventSequenceMutationRegistryError.Unknown && Validation is null,
            EventSequenceMutationBeginOutcome.Archived =>
                Active is null && Token is null && History is not null && ConflictingMutationId is null &&
                Error == EventSequenceMutationRegistryError.Unknown && Validation is null,
            _ => false
        };

    /// <summary>Creates a result for a newly reserved mutation.</summary>
    /// <param name="active">The reserved active mutation.</param>
    /// <param name="token">The complete state token.</param>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationBeginResult Reserved(EventSequenceMutation active, EventSequenceMutationStateToken token) =>
        ActiveSuccess(EventSequenceMutationBeginOutcome.Reserved, active, token);

    /// <summary>Creates a result for an exact active mutation that was resumed.</summary>
    /// <param name="active">The resumed active mutation.</param>
    /// <param name="token">The complete state token.</param>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationBeginResult Resumed(EventSequenceMutation active, EventSequenceMutationStateToken token) =>
        ActiveSuccess(EventSequenceMutationBeginOutcome.Resumed, active, token);

    /// <summary>Creates a result for a recovered permanent reservation.</summary>
    /// <param name="active">The recovered active mutation.</param>
    /// <param name="token">The complete state token.</param>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationBeginResult RecoveredReservation(EventSequenceMutation active, EventSequenceMutationStateToken token) =>
        ActiveSuccess(EventSequenceMutationBeginOutcome.RecoveredReservation, active, token);

    /// <summary>Creates a result for an exact mutation that was already archived.</summary>
    /// <param name="scope">The exact event sequence scope.</param>
    /// <param name="registration">The permanent archived registration.</param>
    /// <param name="history">The verified payload-free terminal history.</param>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationBeginResult Archived(
        EventSequenceKey scope,
        EventSequenceMutationRegistration registration,
        EventSequenceMutationHistoryEntry history)
    {
        EventSequenceMutationRegistryResultGuard.RequireArchivedPayload(scope, registration, history);
        return new(EventSequenceMutationBeginOutcome.Archived, null, null, history, null, EventSequenceMutationRegistryError.Unknown, null);
    }

    /// <summary>Creates a result for a target with another active mutation.</summary>
    /// <param name="mutationId">The non-sensitive identity of the active mutation.</param>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationBeginResult MutationAlreadyInProgress(EventSequenceMutationId mutationId) =>
        Conflict(EventSequenceMutationBeginOutcome.MutationAlreadyInProgress, mutationId, EventSequenceMutationRegistryError.MutationAlreadyInProgress);

    /// <summary>Creates a result for an identifier permanently bound to another request.</summary>
    /// <param name="mutationId">The non-sensitive conflicting mutation identity.</param>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationBeginResult DefinitionConflict(EventSequenceMutationId mutationId) =>
        Conflict(EventSequenceMutationBeginOutcome.DefinitionConflict, mutationId, EventSequenceMutationRegistryError.DefinitionConflict);

    /// <summary>Creates a bounded contention result.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationBeginResult Contended() => Failure(EventSequenceMutationBeginOutcome.Contended, EventSequenceMutationRegistryError.Contended);

    /// <summary>Creates an indeterminate result.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationBeginResult Indeterminate() => Failure(EventSequenceMutationBeginOutcome.Indeterminate, EventSequenceMutationRegistryError.Indeterminate);

    /// <summary>Creates an invalid-input result.</summary>
    /// <param name="validation">The explicit failed validation result.</param>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationBeginResult Invalid(EventSequenceMutationValidationResult validation)
    {
        EventSequenceMutationRegistryResultGuard.RequireInvalid(validation);
        return new(EventSequenceMutationBeginOutcome.Invalid, null, null, null, null, EventSequenceMutationRegistryError.Invalid, validation);
    }

    /// <summary>Creates a corrupt-state result.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationBeginResult Corrupt() => Failure(EventSequenceMutationBeginOutcome.Corrupt, EventSequenceMutationRegistryError.Corrupt);

    /// <summary>Creates an unsupported-provider result.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationBeginResult Unsupported() => Failure(EventSequenceMutationBeginOutcome.Unsupported, EventSequenceMutationRegistryError.Unsupported);

    /// <inheritdoc />
    public override string ToString() => $"{nameof(EventSequenceMutationBeginResult)}: {Outcome} ({Error})";

    static EventSequenceMutationBeginResult ActiveSuccess(EventSequenceMutationBeginOutcome outcome, EventSequenceMutation active, EventSequenceMutationStateToken token)
    {
        EventSequenceMutationRegistryResultGuard.RequireActivePayload(active, token);
        return new(outcome, active, token, null, null, EventSequenceMutationRegistryError.Unknown, null);
    }

    static EventSequenceMutationBeginResult Conflict(
        EventSequenceMutationBeginOutcome outcome,
        EventSequenceMutationId mutationId,
        EventSequenceMutationRegistryError error)
    {
        EventSequenceMutationRegistryResultGuard.RequireMutationId(mutationId);
        return new(outcome, null, null, null, mutationId, error, null);
    }

    static EventSequenceMutationBeginResult Failure(EventSequenceMutationBeginOutcome outcome, EventSequenceMutationRegistryError error) =>
        new(outcome, null, null, null, null, error, null);

    static void Validate(
        EventSequenceMutationBeginOutcome outcome,
        EventSequenceMutation? active,
        EventSequenceMutationStateToken? token,
        EventSequenceMutationHistoryEntry? history,
        EventSequenceMutationId? conflictingMutationId,
        EventSequenceMutationRegistryError error,
        EventSequenceMutationValidationResult? validation)
    {
        var activeSuccess = outcome is EventSequenceMutationBeginOutcome.Reserved or EventSequenceMutationBeginOutcome.Resumed or EventSequenceMutationBeginOutcome.RecoveredReservation;
        var hasActivePayload = active is not null && token is not null;
        var archived = outcome == EventSequenceMutationBeginOutcome.Archived;
        var hasConflictIdentity = conflictingMutationId is not null;
        var hasHistory = history is not null;
        var hasValidation = validation is not null;
        EventSequenceMutationRegistryResultGuard.Require(activeSuccess == hasActivePayload, nameof(active));
        EventSequenceMutationRegistryResultGuard.Require(archived == hasHistory, nameof(history));
        EventSequenceMutationRegistryResultGuard.Require(
            (outcome is EventSequenceMutationBeginOutcome.MutationAlreadyInProgress or EventSequenceMutationBeginOutcome.DefinitionConflict) == hasConflictIdentity,
            nameof(conflictingMutationId));
        EventSequenceMutationRegistryResultGuard.Require(outcome == EventSequenceMutationBeginOutcome.Invalid == hasValidation, nameof(validation));

        var expectedError = outcome switch
        {
            EventSequenceMutationBeginOutcome.Reserved or EventSequenceMutationBeginOutcome.Resumed or EventSequenceMutationBeginOutcome.RecoveredReservation or EventSequenceMutationBeginOutcome.Archived => EventSequenceMutationRegistryError.Unknown,
            EventSequenceMutationBeginOutcome.MutationAlreadyInProgress => EventSequenceMutationRegistryError.MutationAlreadyInProgress,
            EventSequenceMutationBeginOutcome.DefinitionConflict => EventSequenceMutationRegistryError.DefinitionConflict,
            EventSequenceMutationBeginOutcome.Contended => EventSequenceMutationRegistryError.Contended,
            EventSequenceMutationBeginOutcome.Indeterminate => EventSequenceMutationRegistryError.Indeterminate,
            EventSequenceMutationBeginOutcome.Invalid => EventSequenceMutationRegistryError.Invalid,
            EventSequenceMutationBeginOutcome.Corrupt => EventSequenceMutationRegistryError.Corrupt,
            EventSequenceMutationBeginOutcome.Unsupported => EventSequenceMutationRegistryError.Unsupported,
            _ => throw new InvalidEventSequenceMutationRegistryResult(nameof(outcome))
        };
        EventSequenceMutationRegistryResultGuard.Require(error == expectedError, nameof(error));
    }
}

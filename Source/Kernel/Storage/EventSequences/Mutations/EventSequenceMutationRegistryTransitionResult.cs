// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents the closed provider result of applying an event sequence mutation transition.
/// </summary>
public sealed class EventSequenceMutationRegistryTransitionResult
{
    EventSequenceMutationRegistryTransitionResult(
        EventSequenceMutationRegistryTransitionOutcome outcome,
        EventSequenceMutation? active,
        EventSequenceMutationStateToken? token,
        EventSequenceMutationHistoryEntry? history,
        EventSequenceMutationRegistryError error,
        EventSequenceMutationValidationResult? validation)
    {
        Outcome = outcome;
        Active = active;
        Token = token;
        History = history;
        Error = error;
        Validation = validation;
    }

    /// <summary>Gets the closed transition outcome.</summary>
    public EventSequenceMutationRegistryTransitionOutcome Outcome { get; }

    /// <summary>Gets the active mutation for an active success.</summary>
    public EventSequenceMutation? Active { get; }

    /// <summary>Gets the complete state token for an active success.</summary>
    public EventSequenceMutationStateToken? Token { get; }

    /// <summary>Gets the verified payload-free history for an archived success.</summary>
    public EventSequenceMutationHistoryEntry? History { get; }

    /// <summary>Gets the non-sensitive typed error for a failed outcome.</summary>
    public EventSequenceMutationRegistryError Error { get; }

    /// <summary>Gets validation details for an invalid outcome.</summary>
    public EventSequenceMutationValidationResult? Validation { get; }

    /// <summary>Gets whether the result contains the exact mandatory success payload.</summary>
    public bool IsSuccess =>
        Outcome switch
        {
            EventSequenceMutationRegistryTransitionOutcome.Applied or EventSequenceMutationRegistryTransitionOutcome.AlreadyApplied =>
                Active is not null && Token is not null && History is null && Error == EventSequenceMutationRegistryError.Unknown && Validation is null,
            EventSequenceMutationRegistryTransitionOutcome.AlreadyArchived =>
                Active is null && Token is null && History is not null && Error == EventSequenceMutationRegistryError.Unknown && Validation is null,
            _ => false
        };

    /// <summary>Creates a result for an applied transition.</summary>
    /// <param name="active">The successor active mutation.</param>
    /// <param name="token">The successor token.</param>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationRegistryTransitionResult Applied(EventSequenceMutation active, EventSequenceMutationStateToken token) =>
        ActiveSuccess(EventSequenceMutationRegistryTransitionOutcome.Applied, active, token);

    /// <summary>Creates a result for an exact transition that was already applied.</summary>
    /// <param name="active">The observed successor mutation.</param>
    /// <param name="token">The observed successor token.</param>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationRegistryTransitionResult AlreadyApplied(EventSequenceMutation active, EventSequenceMutationStateToken token) =>
        ActiveSuccess(EventSequenceMutationRegistryTransitionOutcome.AlreadyApplied, active, token);

    /// <summary>Creates a result for a mutation that was already archived.</summary>
    /// <param name="scope">The exact event sequence scope.</param>
    /// <param name="registration">The permanent archived registration.</param>
    /// <param name="history">The verified payload-free terminal history.</param>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationRegistryTransitionResult AlreadyArchived(
        EventSequenceKey scope,
        EventSequenceMutationRegistration registration,
        EventSequenceMutationHistoryEntry history)
    {
        EventSequenceMutationRegistryResultGuard.RequireArchivedPayload(scope, registration, history);
        return new(EventSequenceMutationRegistryTransitionOutcome.AlreadyArchived, null, null, history, EventSequenceMutationRegistryError.Unknown, null);
    }

    /// <summary>Creates a result for a state or token conflict.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationRegistryTransitionResult StateConflict() =>
        Failure(EventSequenceMutationRegistryTransitionOutcome.StateConflict, EventSequenceMutationRegistryError.StateConflict);

    /// <summary>Creates a bounded contention result.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationRegistryTransitionResult Contended() => Failure(EventSequenceMutationRegistryTransitionOutcome.Contended, EventSequenceMutationRegistryError.Contended);

    /// <summary>Creates an indeterminate result.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationRegistryTransitionResult Indeterminate() => Failure(EventSequenceMutationRegistryTransitionOutcome.Indeterminate, EventSequenceMutationRegistryError.Indeterminate);

    /// <summary>Creates an invalid-input result.</summary>
    /// <param name="validation">The explicit failed validation result.</param>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationRegistryTransitionResult Invalid(EventSequenceMutationValidationResult validation)
    {
        EventSequenceMutationRegistryResultGuard.RequireInvalid(validation);
        return new(EventSequenceMutationRegistryTransitionOutcome.Invalid, null, null, null, EventSequenceMutationRegistryError.Invalid, validation);
    }

    /// <summary>Creates a corrupt-state result.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationRegistryTransitionResult Corrupt() => Failure(EventSequenceMutationRegistryTransitionOutcome.Corrupt, EventSequenceMutationRegistryError.Corrupt);

    /// <summary>Creates an unsupported-provider result.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationRegistryTransitionResult Unsupported() => Failure(EventSequenceMutationRegistryTransitionOutcome.Unsupported, EventSequenceMutationRegistryError.Unsupported);

    /// <inheritdoc />
    public override string ToString() => $"{nameof(EventSequenceMutationRegistryTransitionResult)}: {Outcome} ({Error})";

    static EventSequenceMutationRegistryTransitionResult ActiveSuccess(
        EventSequenceMutationRegistryTransitionOutcome outcome,
        EventSequenceMutation active,
        EventSequenceMutationStateToken token)
    {
        EventSequenceMutationRegistryResultGuard.RequireActivePayload(active, token);
        return new(outcome, active, token, null, EventSequenceMutationRegistryError.Unknown, null);
    }

    static EventSequenceMutationRegistryTransitionResult Failure(
        EventSequenceMutationRegistryTransitionOutcome outcome,
        EventSequenceMutationRegistryError error) =>
        new(outcome, null, null, null, error, null);
}

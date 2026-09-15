// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents the closed provider result of archiving an event sequence mutation.
/// </summary>
public sealed class EventSequenceMutationRegistryArchiveResult
{
    EventSequenceMutationRegistryArchiveResult(
        EventSequenceMutationRegistryArchiveOutcome outcome,
        EventSequenceMutationHistoryEntry? history,
        EventSequenceMutationRegistryError error,
        EventSequenceMutationValidationResult? validation)
    {
        Outcome = outcome;
        History = history;
        Error = error;
        Validation = validation;
    }

    /// <summary>Gets the closed archive outcome.</summary>
    public EventSequenceMutationRegistryArchiveOutcome Outcome { get; }

    /// <summary>Gets the exact verified payload-free history for an archived success.</summary>
    public EventSequenceMutationHistoryEntry? History { get; }

    /// <summary>Gets the non-sensitive typed error for a failed outcome.</summary>
    public EventSequenceMutationRegistryError Error { get; }

    /// <summary>Gets validation details for an invalid outcome.</summary>
    public EventSequenceMutationValidationResult? Validation { get; }

    /// <summary>Gets whether the result contains the exact mandatory success payload.</summary>
    public bool IsSuccess =>
        Outcome is EventSequenceMutationRegistryArchiveOutcome.Archived or EventSequenceMutationRegistryArchiveOutcome.AlreadyArchived &&
        History is not null && Error == EventSequenceMutationRegistryError.Unknown && Validation is null;

    /// <summary>Creates a result for a newly archived mutation.</summary>
    /// <param name="scope">The exact event sequence scope.</param>
    /// <param name="registration">The permanent archived registration.</param>
    /// <param name="history">The verified payload-free terminal history.</param>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationRegistryArchiveResult Archived(
        EventSequenceKey scope,
        EventSequenceMutationRegistration registration,
        EventSequenceMutationHistoryEntry history) =>
        Success(EventSequenceMutationRegistryArchiveOutcome.Archived, scope, registration, history);

    /// <summary>Creates a result for an exact mutation that was already archived.</summary>
    /// <param name="scope">The exact event sequence scope.</param>
    /// <param name="registration">The permanent archived registration.</param>
    /// <param name="history">The verified payload-free terminal history.</param>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationRegistryArchiveResult AlreadyArchived(
        EventSequenceKey scope,
        EventSequenceMutationRegistration registration,
        EventSequenceMutationHistoryEntry history) =>
        Success(EventSequenceMutationRegistryArchiveOutcome.AlreadyArchived, scope, registration, history);

    /// <summary>
    /// Creates an exact archive retry result without retaining the original command payload.
    /// </summary>
    /// <param name="scope">The requested scope.</param>
    /// <param name="token">The exact archive predecessor token.</param>
    /// <param name="history">The durable terminal receipt.</param>
    /// <returns>The verified retry result, or a state conflict.</returns>
    public static EventSequenceMutationRegistryArchiveResult FromReceipt(
        EventSequenceKey scope,
        EventSequenceMutationStateToken token,
        EventSequenceMutationHistoryEntry history) =>
        EventSequenceMutationReceipt.IsExactArchiveRetry(scope, token, history)
            ? new(EventSequenceMutationRegistryArchiveOutcome.AlreadyArchived, history, EventSequenceMutationRegistryError.Unknown, null)
            : StateConflict();

    /// <summary>Creates a result for a state or token conflict.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationRegistryArchiveResult StateConflict() =>
        Failure(EventSequenceMutationRegistryArchiveOutcome.StateConflict, EventSequenceMutationRegistryError.StateConflict);

    /// <summary>Creates a bounded contention result.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationRegistryArchiveResult Contended() => Failure(EventSequenceMutationRegistryArchiveOutcome.Contended, EventSequenceMutationRegistryError.Contended);

    /// <summary>Creates an indeterminate result.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationRegistryArchiveResult Indeterminate() => Failure(EventSequenceMutationRegistryArchiveOutcome.Indeterminate, EventSequenceMutationRegistryError.Indeterminate);

    /// <summary>Creates an invalid-input result.</summary>
    /// <param name="validation">The explicit failed validation result.</param>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationRegistryArchiveResult Invalid(EventSequenceMutationValidationResult validation)
    {
        EventSequenceMutationRegistryResultGuard.RequireInvalid(validation);
        return new(EventSequenceMutationRegistryArchiveOutcome.Invalid, null, EventSequenceMutationRegistryError.Invalid, validation);
    }

    /// <summary>Creates a corrupt-state result.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationRegistryArchiveResult Corrupt() => Failure(EventSequenceMutationRegistryArchiveOutcome.Corrupt, EventSequenceMutationRegistryError.Corrupt);

    /// <summary>Creates an unsupported-provider result.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationRegistryArchiveResult Unsupported() => Failure(EventSequenceMutationRegistryArchiveOutcome.Unsupported, EventSequenceMutationRegistryError.Unsupported);

    /// <inheritdoc />
    public override string ToString() => $"{nameof(EventSequenceMutationRegistryArchiveResult)}: {Outcome} ({Error})";

    static EventSequenceMutationRegistryArchiveResult Success(
        EventSequenceMutationRegistryArchiveOutcome outcome,
        EventSequenceKey scope,
        EventSequenceMutationRegistration registration,
        EventSequenceMutationHistoryEntry history)
    {
        EventSequenceMutationRegistryResultGuard.RequireArchivedPayload(scope, registration, history);
        return new(outcome, history, EventSequenceMutationRegistryError.Unknown, null);
    }

    static EventSequenceMutationRegistryArchiveResult Failure(
        EventSequenceMutationRegistryArchiveOutcome outcome,
        EventSequenceMutationRegistryError error) =>
        new(outcome, null, error, null);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents the closed result of beginning mutation tracking for an event sequence.
/// </summary>
public sealed class EventSequenceMutationTrackingResult
{
    EventSequenceMutationTrackingResult(
        EventSequenceMutationTrackingOutcome outcome,
        EventSequenceMutationCoverage? coverage,
        EventSequenceMutationRegistryError error,
        EventSequenceMutationValidationResult? validation)
    {
        Outcome = outcome;
        Coverage = coverage;
        Error = error;
        Validation = validation;
    }

    /// <summary>Gets the closed tracking outcome.</summary>
    public EventSequenceMutationTrackingOutcome Outcome { get; }

    /// <summary>Gets the resulting or observed non-sensitive coverage.</summary>
    public EventSequenceMutationCoverage? Coverage { get; }

    /// <summary>Gets the non-sensitive typed error for a failed outcome.</summary>
    public EventSequenceMutationRegistryError Error { get; }

    /// <summary>Gets validation details for an invalid outcome.</summary>
    public EventSequenceMutationValidationResult? Validation { get; }

    /// <summary>Gets whether tracking began or was already active.</summary>
    public bool IsSuccess =>
        Outcome is EventSequenceMutationTrackingOutcome.Began or EventSequenceMutationTrackingOutcome.AlreadyTracking &&
        Coverage == EventSequenceMutationCoverage.Unsealed &&
        Error == EventSequenceMutationRegistryError.Unknown &&
        Validation is null;

    /// <summary>Creates a result for tracking that began.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationTrackingResult Began() => Success(EventSequenceMutationTrackingOutcome.Began);

    /// <summary>Creates a result for tracking that was already active.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationTrackingResult AlreadyTracking() => Success(EventSequenceMutationTrackingOutcome.AlreadyTracking);

    /// <summary>Creates a result for an expected-coverage conflict.</summary>
    /// <param name="observedCoverage">The non-sensitive observed coverage.</param>
    /// <returns>The closed result.</returns>
    /// <exception cref="InvalidEventSequenceMutationRegistryResult">Thrown when the coverage value is undefined.</exception>
    public static EventSequenceMutationTrackingResult Conflict(EventSequenceMutationCoverage observedCoverage)
    {
        if (!Enum.IsDefined(observedCoverage))
        {
            throw new InvalidEventSequenceMutationRegistryResult(nameof(observedCoverage));
        }

        return new(EventSequenceMutationTrackingOutcome.Conflict, observedCoverage, EventSequenceMutationRegistryError.TrackingConflict, null);
    }

    /// <summary>Creates a bounded contention result.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationTrackingResult Contended() => Failure(EventSequenceMutationTrackingOutcome.Contended, EventSequenceMutationRegistryError.Contended);

    /// <summary>Creates an indeterminate result.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationTrackingResult Indeterminate() => Failure(EventSequenceMutationTrackingOutcome.Indeterminate, EventSequenceMutationRegistryError.Indeterminate);

    /// <summary>Creates an invalid-input result.</summary>
    /// <param name="validation">The explicit failed validation result.</param>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationTrackingResult Invalid(EventSequenceMutationValidationResult validation)
    {
        EventSequenceMutationRegistryResultGuard.RequireInvalid(validation);
        return new(EventSequenceMutationTrackingOutcome.Invalid, null, EventSequenceMutationRegistryError.Invalid, validation);
    }

    /// <summary>Creates a corrupt-state result.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationTrackingResult Corrupt() => Failure(EventSequenceMutationTrackingOutcome.Corrupt, EventSequenceMutationRegistryError.Corrupt);

    /// <summary>Creates an unsupported-provider result.</summary>
    /// <returns>The closed result.</returns>
    public static EventSequenceMutationTrackingResult Unsupported() => Failure(EventSequenceMutationTrackingOutcome.Unsupported, EventSequenceMutationRegistryError.Unsupported);

    /// <inheritdoc />
    public override string ToString() => $"{nameof(EventSequenceMutationTrackingResult)}: {Outcome} ({Error})";

    static EventSequenceMutationTrackingResult Success(EventSequenceMutationTrackingOutcome outcome) =>
        new(outcome, EventSequenceMutationCoverage.Unsealed, EventSequenceMutationRegistryError.Unknown, null);

    static EventSequenceMutationTrackingResult Failure(EventSequenceMutationTrackingOutcome outcome, EventSequenceMutationRegistryError error) =>
        new(outcome, null, error, null);
}

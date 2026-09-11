// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents the closed result of a pure mutation state transition.
/// </summary>
public sealed class EventSequenceMutationTransitionResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceMutationTransitionResult"/> class.
    /// </summary>
    /// <param name="outcome">The transition outcome.</param>
    /// <param name="mutation">The observed mutation.</param>
    /// <param name="token">The successor token.</param>
    /// <param name="validation">The validation result.</param>
    internal EventSequenceMutationTransitionResult(
        EventSequenceMutationTransitionOutcome outcome,
        EventSequenceMutation? mutation,
        EventSequenceMutationStateToken? token,
        EventSequenceMutationValidationResult validation)
    {
        Outcome = outcome;
        Mutation = mutation;
        Token = token;
        Validation = validation;
    }

    /// <summary>
    /// Gets the transition outcome.
    /// </summary>
    public EventSequenceMutationTransitionOutcome Outcome { get; }

    /// <summary>
    /// Gets the successor for applied outcomes, the current mutation for conflicts, or null for invalid input.
    /// </summary>
    public EventSequenceMutation? Mutation { get; }

    /// <summary>
    /// Gets the successor token for applied outcomes.
    /// </summary>
    public EventSequenceMutationStateToken? Token { get; }

    /// <summary>
    /// Gets the validation result.
    /// </summary>
    public EventSequenceMutationValidationResult Validation { get; }

    /// <summary>
    /// Gets whether the result contains the mandatory successful payload.
    /// </summary>
    public bool IsSuccess =>
        Outcome is EventSequenceMutationTransitionOutcome.Applied or EventSequenceMutationTransitionOutcome.AlreadyApplied &&
        Mutation is not null &&
        Token is not null &&
        Validation.IsValid;
}

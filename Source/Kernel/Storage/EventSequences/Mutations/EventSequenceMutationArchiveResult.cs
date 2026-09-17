// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents the closed result of preparing a terminal event sequence mutation receipt.
/// </summary>
public sealed class EventSequenceMutationArchiveResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceMutationArchiveResult"/> class.
    /// </summary>
    /// <param name="outcome">The archive preparation outcome.</param>
    /// <param name="history">The prepared terminal receipt.</param>
    /// <param name="validation">The validation result.</param>
    internal EventSequenceMutationArchiveResult(
        EventSequenceMutationArchiveOutcome outcome,
        EventSequenceMutationHistoryEntry? history,
        EventSequenceMutationValidationResult validation)
    {
        Outcome = outcome;
        History = history;
        Validation = validation;
    }

    /// <summary>
    /// Gets the archive preparation outcome.
    /// </summary>
    public EventSequenceMutationArchiveOutcome Outcome { get; }

    /// <summary>
    /// Gets the prepared terminal receipt for a successful outcome.
    /// </summary>
    public EventSequenceMutationHistoryEntry? History { get; }

    /// <summary>
    /// Gets the validation result.
    /// </summary>
    public EventSequenceMutationValidationResult Validation { get; }

    /// <summary>
    /// Gets whether a valid terminal receipt was prepared.
    /// </summary>
    public bool IsSuccess =>
        Outcome == EventSequenceMutationArchiveOutcome.Prepared &&
        History is not null &&
        Validation.IsValid;
}

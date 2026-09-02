// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents the result of validating an event sequence mutation value.
/// </summary>
public sealed class EventSequenceMutationValidationResult
{
    /// <summary>
    /// Gets the explicitly produced successful validation result.
    /// </summary>
    public static readonly EventSequenceMutationValidationResult Valid = new(EventSequenceMutationValidationError.None, string.Empty);

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceMutationValidationResult"/> class.
    /// </summary>
    /// <param name="error">The validation error.</param>
    /// <param name="field">The field associated with the error.</param>
    internal EventSequenceMutationValidationResult(EventSequenceMutationValidationError error, string field)
    {
        Error = error;
        Field = field;
    }

    /// <summary>
    /// Gets the validation error, or <see cref="EventSequenceMutationValidationError.None"/>.
    /// </summary>
    public EventSequenceMutationValidationError Error { get; }

    /// <summary>
    /// Gets the field associated with the error.
    /// </summary>
    public string Field { get; }

    /// <summary>
    /// Gets whether validation succeeded.
    /// </summary>
    public bool IsValid => ReferenceEquals(this, Valid);

    /// <summary>
    /// Throws a typed exception when validation failed.
    /// </summary>
    /// <exception cref="InvalidEventSequenceMutation">Thrown when this result is invalid.</exception>
    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            throw new InvalidEventSequenceMutation(this);
        }
    }
}

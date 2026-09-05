// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// The exception that is thrown when an event sequence mutation value is malformed.
/// </summary>
/// <param name="validation">The validation failure.</param>
public class InvalidEventSequenceMutation(EventSequenceMutationValidationResult validation) : Exception($"The event sequence mutation field '{validation.Field}' is invalid: {validation.Error}.")
{
    /// <summary>
    /// Gets the validation failure.
    /// </summary>
    public EventSequenceMutationValidationResult Validation { get; } = validation;
}

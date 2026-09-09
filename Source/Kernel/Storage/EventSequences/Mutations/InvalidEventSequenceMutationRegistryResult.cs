// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// The exception that is thrown when construction of a registry result violates its payload matrix.
/// </summary>
/// <param name="field">The missing or contradictory result field.</param>
public class InvalidEventSequenceMutationRegistryResult(string field) : Exception($"The event sequence mutation registry result field '{field}' is missing or contradictory.")
{
    /// <summary>
    /// Gets the missing or contradictory result field.
    /// </summary>
    public string Field { get; } = field;
}

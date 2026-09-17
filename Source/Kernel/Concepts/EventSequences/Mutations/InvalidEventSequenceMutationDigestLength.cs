// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations;

/// <summary>
/// The exception that is thrown when an event sequence mutation digest does not contain exactly 32 bytes.
/// </summary>
/// <param name="digestType">The digest type being created.</param>
/// <param name="actualLength">The supplied digest length.</param>
public class InvalidEventSequenceMutationDigestLength(Type digestType, int actualLength) :
    Exception($"The mutation digest '{digestType.Name}' must contain exactly 32 bytes, but {actualLength} bytes were supplied.")
{
    /// <summary>
    /// Gets the supplied digest length.
    /// </summary>
    public int ActualLength { get; } = actualLength;
}

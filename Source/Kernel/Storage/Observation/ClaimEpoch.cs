// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents the epoch of a partition ownership grant.
/// </summary>
/// <param name="Value">The epoch value.</param>
public record ClaimEpoch(ulong Value) : ConceptAs<ulong>(Value)
{
    /// <summary>
    /// Gets the epoch representing no grant.
    /// </summary>
    public static readonly ClaimEpoch None = 0;

    /// <summary>
    /// Converts an unsigned integer to a claim epoch.
    /// </summary>
    /// <param name="value">The epoch value.</param>
    /// <returns>The claim epoch.</returns>
    public static implicit operator ClaimEpoch(ulong value) => new(value);
}

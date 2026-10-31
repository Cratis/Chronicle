// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents the revision of an observer's shadow ownership ledger.
/// </summary>
/// <param name="Value">The revision value.</param>
public record OwnershipRevision(ulong Value) : ConceptAs<ulong>(Value)
{
    /// <summary>
    /// Gets the revision representing no ownership transitions.
    /// </summary>
    public static readonly OwnershipRevision None = 0;

    /// <summary>
    /// Converts an unsigned integer to an ownership revision.
    /// </summary>
    /// <param name="value">The revision value.</param>
    /// <returns>The ownership revision.</returns>
    public static implicit operator OwnershipRevision(ulong value) => new(value);

    /// <summary>
    /// Advances to the next ownership revision.
    /// </summary>
    /// <returns>The next revision.</returns>
    public OwnershipRevision Next() => new(Value + 1);
}

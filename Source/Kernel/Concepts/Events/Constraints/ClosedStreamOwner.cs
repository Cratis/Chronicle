// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Identifies the manual operator or closing constraint owning a closure.
/// </summary>
/// <param name="Value">The owner name; empty for manual closures.</param>
public record ClosedStreamOwner(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the owner of manual closures.
    /// </summary>
    public static readonly ClosedStreamOwner Manual = new(string.Empty);

    /// <summary>
    /// Convert a name to a closed stream owner.
    /// </summary>
    /// <param name="value">The owner name.</param>
    /// <returns>The owner.</returns>
    public static implicit operator ClosedStreamOwner(string value) => new(value);
}

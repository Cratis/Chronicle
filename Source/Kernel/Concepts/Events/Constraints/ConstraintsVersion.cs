// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Represents a stamp identifying a specific revision of the registered constraint definitions for an event store.
/// </summary>
/// <param name="Value">The inner value.</param>
/// <remarks>
/// The value is derived from the content of the registered definitions, so it changes whenever a definition is added or
/// changed and is identical for identical definitions — even across process boundaries and grain reactivation. This
/// makes it a cheap, cluster-safe signal for callers to detect that the constraints have changed since they last read
/// them.
/// </remarks>
public record ConstraintsVersion(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="ConstraintsVersion"/>.
    /// </summary>
    public static readonly ConstraintsVersion NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly convert from a <see cref="string"/> to a <see cref="ConstraintsVersion"/>.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    public static implicit operator ConstraintsVersion(string value) => new(value);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Represents the unique identifier of an <see cref="InFlightEvent"/> entry.
/// </summary>
/// <param name="Value">The inner value.</param>
public record InFlightEventId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Gets the representation of a not-set <see cref="InFlightEventId"/>.
    /// </summary>
    public static readonly InFlightEventId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from a <see cref="Guid"/> to an <see cref="InFlightEventId"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> value.</param>
    public static implicit operator InFlightEventId(Guid value) => new(value);

    /// <summary>
    /// Implicitly convert from an <see cref="InFlightEventId"/> to a <see cref="Guid"/>.
    /// </summary>
    /// <param name="id">The <see cref="InFlightEventId"/>.</param>
    public static implicit operator Guid(InFlightEventId id) => id.Value;

    /// <summary>
    /// Creates a new instance of <see cref="InFlightEventId"/> with a new <see cref="Guid"/>.
    /// </summary>
    /// <returns>A new <see cref="InFlightEventId"/>.</returns>
    public static InFlightEventId New() => new(Guid.NewGuid());
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Represents the type of an event stream.
/// </summary>
/// <param name="Value">Actual Value.</param>
public record EventStreamType(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="EventStreamType"/>.
    /// </summary>
    public static readonly EventStreamType All = "All";

    /// <summary>
    /// Gets the representation of an <see cref="EventStreamType"/> representing aggregate roots.
    /// </summary>
    public static readonly EventStreamType AggregateRoot = "AggregateRoot";

    /// <summary>
    /// Check whether or not the <see cref="EventStreamType"/> is the all type.
    /// </summary>
    public bool IsAll => this == All;

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="EventStreamType"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> to convert from.</param>
    /// <returns>A converted <see cref="EventStreamType"/>.</returns>;
    public static implicit operator EventStreamType(string value) => new(value);
}

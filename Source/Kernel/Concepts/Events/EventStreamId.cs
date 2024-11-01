// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Represents the unique identifier of an event stream within a <see cref="EventStreamType"/> .
/// </summary>
/// <param name="Value">Actual value.</param>
public record EventStreamId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="EventStreamId"/>.
    /// </summary>
    public static readonly string Default = "Default";

    /// <summary>
    /// Check whether or not the <see cref="EventStreamId"/> is the default.
    /// </summary>
    public bool IsDefault => this != Default;

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="EventStreamId"/>.
    /// </summary>
    /// <param name="id"><see cref="string"/> to convert from.</param>
    /// <returns>A converted <see cref="EventStreamId"/>.</returns>;
    public static implicit operator EventStreamId(string id) => new(id);
}

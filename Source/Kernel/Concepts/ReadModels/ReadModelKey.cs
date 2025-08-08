// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.ReadModels;

/// <summary>
/// Represents the unique identifier of an instance of an event source.
/// </summary>
/// <param name="Value">Actual value.</param>
public record ReadModelKey(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="ReadModelKey"/>.
    /// </summary>
    public static readonly ReadModelKey Unspecified = new("*");

    /// <summary>
    /// Check whether or not the <see cref="ReadModelKey"/> is specified.
    /// </summary>
    public bool IsSpecified => this != Unspecified;

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="ReadModelKey"/>.
    /// </summary>
    /// <param name="id"><see cref="string"/> to convert from.</param>
    /// <returns>A converted <see cref="ReadModelKey"/>.</returns>;
    public static implicit operator ReadModelKey(string id) => new(id);

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="ReadModelKey"/>.
    /// </summary>
    /// <param name="id"><see cref="Guid"/> to convert from.</param>
    /// <returns>A converted <see cref="ReadModelKey"/>.</returns>;
    public static implicit operator ReadModelKey(Guid id) => new(id.ToString());

    /// <summary>
    /// Implicitly convert from <see cref="EventSourceId"/> to <see cref="ReadModelKey"/>.
    /// </summary>
    /// <param name="id"><see cref="EventSourceId"/> to convert from.</param>
    /// <returns>A converted <see cref="ReadModelKey"/>.</returns>;
    public static implicit operator ReadModelKey(EventSourceId id) => new(id.Value);

    /// <summary>
    /// Implicitly convert from <see cref="ReadModelKey"/> to <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="id"><see cref="ReadModelKey"/> to convert from.</param>
    /// <returns>A converted <see cref="EventSourceId"/>.</returns>;
    public static implicit operator EventSourceId(ReadModelKey id) => new(id.Value);
}

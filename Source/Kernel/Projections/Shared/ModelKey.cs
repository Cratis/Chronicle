// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents the unique identifier of an instance of an event source.
/// </summary>
/// <param name="Value">Actual value.</param>
public record ModelKey(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="ModelKey"/>.
    /// </summary>
    public static readonly ModelKey Unspecified = new("*");

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="ModelKey"/>.
    /// </summary>
    /// <param name="id"><see cref="string"/> to convert from.</param>
    /// <returns>A converted <see cref="ModelKey"/>.</returns>;
    public static implicit operator ModelKey(string id) => new(id);

    /// <summary>
    /// Implicitly convert from <see cref="EventSourceId"/> to <see cref="ModelKey"/>.
    /// </summary>
    /// <param name="id"><see cref="EventSourceId"/> to convert from.</param>
    /// <returns>A converted <see cref="ModelKey"/>.</returns>;
    public static implicit operator ModelKey(EventSourceId id) => new(id.Value);

    /// <summary>
    /// Implicitly convert from <see cref="ModelKey"/> to <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="id"><see cref="ModelKey"/> to convert from.</param>
    /// <returns>A converted <see cref="EventSourceId"/>.</returns>;
    public static implicit operator EventSourceId(ModelKey id) => new(id.Value);

    /// <summary>
    /// Check whether or not the <see cref="ModelKey"/> is specified.
    /// </summary>
    public bool IsSpecified => this != Unspecified;
}

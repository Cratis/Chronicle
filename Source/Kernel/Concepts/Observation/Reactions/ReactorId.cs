// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Reactors;

/// <summary>
/// Represents the unique identifier of a reducer.
/// </summary>
/// <param name="value">The actual value.</param>
public record ReactorId(string value) : ConceptAs<string>(value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="ReactorId"/>.
    /// </summary>
    public static readonly ReactorId Unspecified = ObserverId.Unspecified;

    /// <summary>
    /// Implicitly convert from a string to <see cref="ReactorId"/>.
    /// </summary>
    /// <param name="id">String to convert from.</param>
    public static implicit operator ReactorId(string id) => new(id);

    /// <summary>
    /// Implicitly convert from <see cref="ReactorId"/> to <see cref="ObserverId"/>.
    /// </summary>
    /// <param name="id"><see cref="ReactorId"/> to convert from.</param>
    public static implicit operator ObserverId(ReactorId id) => new(id.Value);

    /// <summary>
    /// Implicitly convert from <see cref="ObserverId"/> to <see cref="ReactorId"/>.
    /// </summary>
    /// <param name="id"><see cref="ObserverId"/> to convert from.</param>
    public static implicit operator ReactorId(ObserverId id) => new(id.Value);
}

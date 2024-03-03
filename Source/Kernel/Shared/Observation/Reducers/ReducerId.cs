// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Observation.Reducers;

/// <summary>
/// Represents the unique identifier of a reducer.
/// </summary>
/// <param name="value">The actual value.</param>
public record ReducerId(Guid value) : ConceptAs<Guid>(value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="ReducerId"/>.
    /// </summary>
    public static readonly ReducerId Unspecified = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from a string representation of a <see cref="Guid"/> to <see cref="ReducerId"/>.
    /// </summary>
    /// <param name="id">String representation of a <see cref="Guid"/> to convert from.</param>
    public static implicit operator ReducerId(string id) => new(Guid.Parse(id));

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="ReducerId"/>.
    /// </summary>
    /// <param name="id"><see cref="Guid"/> to convert from.</param>
    public static implicit operator ReducerId(Guid id) => new(id);

    /// <summary>
    /// Implicitly convert from <see cref="ReducerId"/> to <see cref="ObserverId"/>.
    /// </summary>
    /// <param name="id"><see cref="ReducerId"/> to convert from.</param>
    public static implicit operator ObserverId(ReducerId id) => new(id.Value);

    /// <summary>
    /// Implicitly convert from <see cref="ObserverId"/> to <see cref="ReducerId"/>.
    /// </summary>
    /// <param name="id"><see cref="ObserverId"/> to convert from.</param>
    public static implicit operator ReducerId(ObserverId id) => new(id.Value);
}

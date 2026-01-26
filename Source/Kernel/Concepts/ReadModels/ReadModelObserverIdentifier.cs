// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Concepts.ReadModels;

/// <summary>
/// Represents the observer identifier for a read model.
/// </summary>
/// <param name="Value">Actual value.</param>
public record ReadModelObserverIdentifier(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="ReadModelObserverIdentifier"/>.
    /// </summary>
    public static readonly ReadModelObserverIdentifier Unspecified = new(string.Empty);

    /// <summary>
    /// Implicitly convert from a string to <see cref="ReadModelObserverIdentifier"/>.
    /// </summary>
    /// <param name="id">String to convert from.</param>
    public static implicit operator ReadModelObserverIdentifier(string id) => new(id);

    /// <summary>
    /// Implicitly convert from <see cref="ReadModelObserverIdentifier"/> to string.
    /// </summary>
    /// <param name="id"><see cref="ReadModelObserverIdentifier"/> to convert from.</param>
    public static implicit operator string(ReadModelObserverIdentifier id) => id.Value;

    /// <summary>
    /// Implicitly convert from <see cref="ObserverId"/> to <see cref="ReadModelObserverIdentifier"/>.
    /// </summary>
    /// <param name="id"><see cref="ObserverId"/> to convert from.</param>
    public static implicit operator ReadModelObserverIdentifier(ObserverId id) => new(id.Value);

    /// <summary>
    /// Implicitly convert from <see cref="ReadModelObserverIdentifier"/> to <see cref="ObserverId"/>.
    /// </summary>
    /// <param name="id"><see cref="ReadModelObserverIdentifier"/> to convert from.</param>
    public static implicit operator ObserverId(ReadModelObserverIdentifier id) => new(id.Value);
}

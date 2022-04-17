// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation;

/// <summary>
/// Concept that represents the name of an observer.
/// </summary>
/// <param name="Value">Actual value.</param>
public record ObserverName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the value for when the name is not specified.
    /// </summary>
    public static readonly ObserverName NotSpecified = "[not specified]";

    /// <summary>
    /// Implicitly convert from a string to <see cref="ObserverId"/>.
    /// </summary>
    /// <param name="name">String  to convert from.</param>
    public static implicit operator ObserverName(string name) => new(name);
}

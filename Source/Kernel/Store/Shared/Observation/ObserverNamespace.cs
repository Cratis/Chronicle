// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation;

/// <summary>
/// Represents an observer namespace, typically used for scoping handler streams.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record ObserverNamespace(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The namespace when it is not set.
    /// </summary>
    public static readonly ObserverNamespace NotSet = new("[not set]");

    /// <summary>
    /// Implicitly convert from string to <see cref="ObserverNamespace"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator ObserverNamespace(string value) => new(value);
}

// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis;

/// <summary>
/// Represents the name of an event store.
/// </summary>
/// <param name="Value">The inner value.</param>
public record EventStoreName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="EventStoreName"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator EventStoreName(string value) => new(value);
}

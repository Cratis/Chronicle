// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis;

/// <summary>
/// Represents the name and identifier of an event store.
/// </summary>
/// <param name="Value">The inner value.</param>
public record EventStoreName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="EventStoreName"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> representation.</param>
    public static implicit operator EventStoreName(string value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="EventStoreName"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> to convert from.</param>
    public static implicit operator string(EventStoreName eventStore) => eventStore.Value;
}

// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis;

/// <summary>
/// Represents the name and identifier of an event store.
/// </summary>
/// <param name="Value">The inner value.</param>
public record EventStore(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="EventStore"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> representation.</param>
    public static implicit operator EventStore(string value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="EventStore"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStore"/> to convert from.</param>
    public static implicit operator string(EventStore eventStore) => eventStore.Value;
}

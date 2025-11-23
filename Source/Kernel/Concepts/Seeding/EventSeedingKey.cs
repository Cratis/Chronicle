// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Seeding;

/// <summary>
/// Represents a key for event seeding.
/// </summary>
/// <param name="EventStore">The name of the event store.</param>
/// <param name="Namespace">The namespace within the event store.</param>
public record EventSeedingKey(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace)
{
    /// <summary>
    /// Gets the empty <see cref="EventSeedingKey"/>.
    /// </summary>
    public static readonly EventSeedingKey NotSet = new(EventStoreName.NotSet, EventStoreNamespaceName.NotSet);

    /// <summary>
    /// Implicitly convert from <see cref="EventSeedingKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="EventSeedingKey"/> to convert from.</param>
    public static implicit operator string(EventSeedingKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(EventStore, Namespace);

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="EventSeedingKey"/> instance.</returns>
    public static EventSeedingKey Parse(string key) => KeyHelper.Parse<EventSeedingKey>(key);
}

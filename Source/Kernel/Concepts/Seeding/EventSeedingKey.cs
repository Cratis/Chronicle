// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Seeding;

/// <summary>
/// Represents a key for event seeding.
/// </summary>
/// <param name="EventStore">The name of the event store.</param>
/// <param name="Namespace">The namespace within the event store. Use EventStoreNamespaceName.NotSet for global seeds.</param>
public record EventSeedingKey(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace)
{
    /// <summary>
    /// Gets the empty <see cref="EventSeedingKey"/>.
    /// </summary>
    public static readonly EventSeedingKey NotSet = new(EventStoreName.NotSet, EventStoreNamespaceName.NotSet);

    /// <summary>
    /// Gets a value indicating whether this is a global seed key.
    /// </summary>
    public bool IsGlobal => Namespace == EventStoreNamespaceName.NotSet;

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

    /// <summary>
    /// Create a global event seeding key.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <returns>A global event seeding key.</returns>
    public static EventSeedingKey ForGlobal(EventStoreName eventStore) => new(eventStore, EventStoreNamespaceName.NotSet);

    /// <summary>
    /// Create a namespace-specific event seeding key.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace name.</param>
    /// <returns>A namespace-specific event seeding key.</returns>
    public static EventSeedingKey ForNamespace(EventStoreName eventStore, EventStoreNamespaceName @namespace) => new(eventStore, @namespace);
}

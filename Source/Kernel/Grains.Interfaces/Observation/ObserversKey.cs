// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Observation;

/// <summary>
/// Represents a key for an observer.
/// </summary>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
public record ObserversKey(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace)
{
    /// <summary>
    /// Gets the empty <see cref="ObserversKey"/>.
    /// </summary>
    public static readonly ObserversKey NotSet = new(EventStoreName.NotSet, EventStoreNamespaceName.NotSet);

    /// <summary>
    /// Implicitly convert from <see cref="ObserversKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ObserverKey"/> to convert from.</param>
    public static implicit operator string(ObserversKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => $"{EventStore}+{Namespace}";

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ObserverKey"/> instance.</returns>
    public static ObserversKey Parse(string key)
    {
        var elements = key.Split('+');
        var eventStore = (EventStoreName)elements[0];
        var @namespace = (EventStoreNamespaceName)elements[1];

        return new(eventStore, @namespace);
    }
}

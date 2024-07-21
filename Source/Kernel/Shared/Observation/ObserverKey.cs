// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactions;

/// <summary>
/// Represents a key for an observer.
/// </summary>
/// <param name="ObserverId">The unique identifier of the observer.</param>
/// <param name="EventStore">The name of the event store.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence.</param>
public record ObserverKey(
    ObserverId ObserverId,
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    EventSequenceId EventSequenceId)
{
    /// <summary>
    /// Gets the empty <see cref="ObserverKey"/>.
    /// </summary>
    public static readonly ObserverKey NotSet = new(ObserverId.Unspecified, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSequenceId.Unspecified);

    /// <summary>
    /// Implicitly convert from <see cref="ObserverKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ObserverKey"/> to convert from.</param>
    public static implicit operator string(ObserverKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{ObserverId}+{EventStore}+{Namespace}+{EventSequenceId}";
    }

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ObserverKey"/> instance.</returns>
    public static ObserverKey Parse(string key)
    {
        var elements = key.Split('+');
        var observerId = (ObserverId)elements[0];
        var eventStore = (EventStoreName)elements[1];
        var @namespace = (EventStoreNamespaceName)elements[2];
        var eventSequenceId = (EventSequenceId)elements[3];

        return new(observerId, eventStore, @namespace, eventSequenceId);
    }
}

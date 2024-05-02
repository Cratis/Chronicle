// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.EventSequences;

namespace Cratis.Projections;

/// <summary>
/// Represents the compound key for a projection.
/// </summary>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="EventSequenceId">The event sequence.</param>
public record ProjectionKey(EventStoreName EventStore, EventStoreNamespaceName Namespace, EventSequenceId EventSequenceId)
{
    /// <summary>
    /// Implicitly convert from <see cref="ProjectionKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ProjectionKey"/> to convert from.</param>
    public static implicit operator string(ProjectionKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => $"{EventStore}+{Namespace}+{EventSequenceId}";

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ProjectionKey"/> instance.</returns>
    public static ProjectionKey Parse(string key)
    {
        var elements = key.Split('+');
        var eventStore = (EventStoreName)elements[0];
        var @namespace = (EventStoreNamespaceName)elements[1];
        var eventSequenceId = (EventSequenceId)elements[2];
        return new(eventStore, @namespace, eventSequenceId);
    }
}

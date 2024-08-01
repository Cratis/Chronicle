// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the compound key for a projection.
/// </summary>
/// <param name="ProjectionId">The projection identifier.</param>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="EventSequenceId">The event sequence.</param>
public record ProjectionKey(ProjectionId ProjectionId, EventStoreName EventStore, EventStoreNamespaceName Namespace, EventSequenceId EventSequenceId)
{
    /// <summary>
    /// Implicitly convert from <see cref="ProjectionKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ProjectionKey"/> to convert from.</param>
    public static implicit operator string(ProjectionKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => $"{ProjectionId}+{EventStore}+{Namespace}+{EventSequenceId}";

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ProjectionKey"/> instance.</returns>
    public static ProjectionKey Parse(string key)
    {
        var elements = key.Split('+');
        var projectionId = (ProjectionId)elements[0];
        var eventStore = (EventStoreName)elements[1];
        var @namespace = (EventStoreNamespaceName)elements[2];
        var eventSequenceId = (EventSequenceId)elements[3];
        return new(projectionId, eventStore, @namespace, eventSequenceId);
    }
}

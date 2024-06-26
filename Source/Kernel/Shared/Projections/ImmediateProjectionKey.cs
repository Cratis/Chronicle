// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Models;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the compound key for an immediate projection.
/// </summary>
/// <param name="EventStore">The event store name.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="ModelKey">The event source identifier.</param>
/// <param name="CorrelationId">The optional correlation identifier.</param>
public record ImmediateProjectionKey(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    EventSequenceId EventSequenceId,
    ModelKey ModelKey,
    CorrelationId? CorrelationId = default)
{
    /// <summary>
    /// Implicitly convert from <see cref="ProjectionKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ImmediateProjectionKey"/> to convert from.</param>
    public static implicit operator string(ImmediateProjectionKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString()
    {
        if (CorrelationId != default)
        {
            return $"{EventStore}+{Namespace}+{EventSequenceId}+{ModelKey}+{CorrelationId}";
        }

        return $"{EventStore}+{Namespace}+{EventSequenceId}+{ModelKey}";
    }

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ProjectionKey"/> instance.</returns>
    public static ImmediateProjectionKey Parse(string key)
    {
        var elements = key.Split('+');
        var eventStore = (EventStoreName)elements[0];
        var @namespace = (EventStoreNamespaceName)elements[1];
        var eventSequenceId = (EventSequenceId)elements[2];
        var modelKey = (ModelKey)elements[3];
        if (elements.Length == 5)
        {
            var correlationId = (CorrelationId)elements[4];
            return new(eventStore, @namespace, eventSequenceId, modelKey, correlationId);
        }
        return new(eventStore, @namespace, eventSequenceId, modelKey);
    }
}

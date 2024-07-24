// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Models;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the compound key for an projection.
/// </summary>
/// <param name="ProjectionId">The projection identifier.</param>
/// <param name="EventStore">The event store name.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="ModelKey">The event source identifier.</param>
/// <param name="SessionId">Optional projection session identifier.</param>
public record ImmediateProjectionKey(
    ProjectionId ProjectionId,
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    EventSequenceId EventSequenceId,
    ModelKey ModelKey,
    ProjectionSessionId? SessionId = default)
{
    /// <summary>
    /// Implicitly convert from <see cref="ProjectionKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ImmediateProjectionKey"/> to convert from.</param>
    public static implicit operator string(ImmediateProjectionKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString()
    {
        if (SessionId != default)
        {
            return $"{ProjectionId}+{EventStore}+{Namespace}+{EventSequenceId}+{ModelKey}+{SessionId}";
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
        var projectionId = (ProjectionId)elements[0];
        var eventStore = (EventStoreName)elements[1];
        var @namespace = (EventStoreNamespaceName)elements[2];
        var eventSequenceId = (EventSequenceId)elements[3];
        var modelKey = (ModelKey)elements[4];
        if (elements.Length == 6)
        {
            var sessionId = (ProjectionSessionId)elements[5];
            return new(projectionId, eventStore, @namespace, eventSequenceId, modelKey, sessionId);
        }
        return new(projectionId, eventStore, @namespace, eventSequenceId, modelKey);
    }
}

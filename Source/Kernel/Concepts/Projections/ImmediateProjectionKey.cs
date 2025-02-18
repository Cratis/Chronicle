// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Models;

namespace Cratis.Chronicle.Concepts.Projections;

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
    /// Implicitly convert from <see cref="ImmediateProjectionKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ImmediateProjectionKey"/> to convert from.</param>
    public static implicit operator string(ImmediateProjectionKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString()
    {
        if (SessionId != default)
        {
            return KeyHelper.Combine(ProjectionId, EventStore, Namespace, EventSequenceId, ModelKey, SessionId);
        }

        return KeyHelper.Combine(ProjectionId, EventStore, Namespace, EventSequenceId, ModelKey);
    }

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ProjectionKey"/> instance.</returns>
    public static ImmediateProjectionKey Parse(string key) => KeyHelper.Parse<ImmediateProjectionKey>(key);
}

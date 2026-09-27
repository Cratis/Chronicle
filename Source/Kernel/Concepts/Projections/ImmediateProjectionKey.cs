// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Concepts.Projections;

/// <summary>
/// Represents the compound key for an projection.
/// </summary>
/// <param name="ProjectionId">The projection identifier.</param>
/// <param name="EventStore">The event store name.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="ReadModelKey">The read model key.</param>
/// <param name="SessionId">Optional projection session identifier.</param>
public record ImmediateProjectionKey(
    ProjectionId ProjectionId,
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    EventSequenceId EventSequenceId,
    ReadModelKey ReadModelKey,
    ProjectionSessionId? SessionId = default)
{
    const string ReadModelKeyMarker = "$read-model-key";

    /// <summary>
    /// Implicitly convert from <see cref="ImmediateProjectionKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ImmediateProjectionKey"/> to convert from.</param>
    public static implicit operator string(ImmediateProjectionKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString()
    {
        if (ReadModelKey.Value.Contains(KeyHelper.Separator))
        {
            // Mark the new layout so a key with one separator cannot be mistaken for an older key with a session.
            // The read-model key is last and can therefore absorb all remaining fragments when parsed.
            return KeyHelper.Combine(ProjectionId, EventStore, Namespace, EventSequenceId, ReadModelKeyMarker, SessionId?.ToString() ?? string.Empty, ReadModelKey);
        }

        if (SessionId != default)
        {
            return KeyHelper.Combine(ProjectionId, EventStore, Namespace, EventSequenceId, ReadModelKey, SessionId);
        }

        return KeyHelper.Combine(ProjectionId, EventStore, Namespace, EventSequenceId, ReadModelKey);
    }

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ProjectionKey"/> instance.</returns>
    public static ImmediateProjectionKey Parse(string key)
    {
        var parts = key.Split(KeyHelper.Separator);
        if (parts.Length >= 8 && parts[4] == ReadModelKeyMarker)
        {
            return new(
                parts[0],
                parts[1],
                parts[2],
                parts[3],
                string.Join(KeyHelper.Separator, parts[6..]),
                string.IsNullOrEmpty(parts[5]) ? null : (ProjectionSessionId)Guid.Parse(parts[5]));
        }

        return KeyHelper.Parse<ImmediateProjectionKey>(key);
    }
}

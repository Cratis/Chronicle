// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Orleans.Aggregates;

/// <summary>
/// Represents a key for an aggregate root.
/// </summary>
/// <param name="EventSource">The <see cref="Events.EventSource"/> part of the key.</param>
/// <param name="EventSourceId">The <see cref="Events.EventSourceId"/> part of the key.</param>
/// <param name="EventStreamId">The <see cref="EventStreamId"/> it belongs to.</param>
public record AggregateRootKey(EventSource EventSource, EventSourceId EventSourceId, EventStreamId EventStreamId)
{
    /// <summary>
    /// Implicitly convert to a string.
    /// </summary>
    /// <param name="key"><see cref="AggregateRootKey"/> to convert from.</param>
    public static implicit operator string(AggregateRootKey key) => key.ToString();

    /// <summary>
    /// Implicitly convert from a string.
    /// </summary>
    /// <param name="key">String representation.</param>
    public static implicit operator AggregateRootKey(string key) => Parse(key);

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(EventSource, EventSourceId, EventStreamId);

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="AggregateRootKey"/> instance.</returns>
    public static AggregateRootKey Parse(string key) => KeyHelper.Parse<AggregateRootKey>(key);
}

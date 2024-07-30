// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

#pragma warning disable SA1402

namespace Cratis.Chronicle.Orleans.Aggregates;

/// <summary>
/// Represents the key for an event sequence.
/// </summary>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> part.</param>
/// <param name="EventStore">The <see cref="EventStoreName"/> part.</param>
/// <param name="Namespace">The <see cref="EventStoreNamespaceName"/> part.</param>
public record AggregateRootKey(EventSourceId EventSourceId, EventStoreName EventStore, EventStoreNamespaceName Namespace)
{
    /// <summary>
    /// The key when not set.
    /// </summary>
    public static readonly AggregateRootKey NotSet = new(EventSourceId.Unspecified, EventStoreName.NotSet, EventStoreNamespaceName.NotSet);

    /// <summary>
    /// Implicitly convert from <see cref="AggregateRootKey"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="key">Key to convert from.</param>
    public static implicit operator string(AggregateRootKey key) => key.ToString();

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="AggregateRootKey"/>.
    /// </summary>
    /// <param name="key">String to convert from.</param>
    public static implicit operator AggregateRootKey(string key) => Parse(key);

    /// <inheritdoc/>
    public override string ToString() => $"{EventSourceId}+{EventStore}+{Namespace}";

    /// <summary>
    /// Parse a <see cref="AggregateRootKey"/> from a string.
    /// </summary>
    /// <param name="key">String to parse.</param>
    /// <returns>A parsed <see cref="AggregateRootKey"/>.</returns>
    public static AggregateRootKey Parse(string key)
    {
        var parts = key.Split('+');
        return new AggregateRootKey(parts[0], parts[1], parts[2]);
    }
}

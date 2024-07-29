// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents the key for an event sequence.
/// </summary>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> part.</param>
/// <param name="EventStore">The <see cref="EventStoreName"/> part.</param>
/// <param name="Namespace">The <see cref="EventStoreNamespaceName"/> part.</param>
public record EventSequenceKey(EventSequenceId EventSequenceId, EventStoreName EventStore, EventStoreNamespaceName Namespace)
{
    /// <summary>
    /// The key when not set.
    /// </summary>
    public static readonly EventSequenceKey NotSet = new(EventSequenceId.Unspecified, EventStoreName.NotSet, EventStoreNamespaceName.NotSet);

    /// <summary>
    /// Implicitly convert from <see cref="EventSequenceKey"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="key">Key to convert from.</param>
    public static implicit operator string(EventSequenceKey key) => key.ToString();

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="EventSequenceKey"/>.
    /// </summary>
    /// <param name="key">String to convert from.</param>
    public static implicit operator EventSequenceKey(string key) => Parse(key);

    /// <inheritdoc/>
    public override string ToString() => $"{EventSequenceId}+{EventStore}+{Namespace}";

    /// <summary>
    /// Parse a <see cref="EventSequenceKey"/> from a string.
    /// </summary>
    /// <param name="key">String to parse.</param>
    /// <returns>A parsed <see cref="EventSequenceKey"/>.</returns>
    public static EventSequenceKey Parse(string key)
    {
        var parts = key.Split('+');
        return new EventSequenceKey(parts[0], parts[1], parts[2]);
    }
}

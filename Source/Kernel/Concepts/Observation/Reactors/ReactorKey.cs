// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Observation.Reactors;

/// <summary>
/// Represents the compound key for a reactor.
/// </summary>
/// <param name="ReactorId">The reducer identifier.</param>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="EventSequenceId">The event sequence.</param>
public record ReactorKey(ReactorId ReactorId, EventStoreName EventStore, EventStoreNamespaceName Namespace, EventSequenceId EventSequenceId)
{
    /// <summary>
    /// Implicitly convert from <see cref="ReactorKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ReactorKey"/> to convert from.</param>
    public static implicit operator string(ReactorKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(ReactorId, EventStore, Namespace, EventSequenceId);

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ReactorKey"/> instance.</returns>
    public static ReactorKey Parse(string key) => KeyHelper.Parse<ReactorKey>(key);
}

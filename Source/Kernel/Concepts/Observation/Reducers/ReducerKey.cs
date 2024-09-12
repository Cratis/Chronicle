// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Observation.Reducers;

/// <summary>
/// Represents the compound key for a reducer.
/// </summary>
/// <param name="ReducerId">The reducer identifier.</param>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="EventSequenceId">The event sequence.</param>
public record ReducerKey(ReducerId ReducerId, EventStoreName EventStore, EventStoreNamespaceName Namespace, EventSequenceId EventSequenceId)
{
    /// <summary>
    /// Implicitly convert from <see cref="ReducerKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ReducerKey"/> to convert from.</param>
    public static implicit operator string(ReducerKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(ReducerId, EventStore, Namespace, EventSequenceId);

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ReducerKey"/> instance.</returns>
    public static ReducerKey Parse(string key) => KeyHelper.Parse<ReducerKey>(key);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Grains.ReadModels;

/// <summary>
/// Represents the key for a read models replay manager.
/// </summary>
/// <param name="EventStore">The <see cref="EventStoreName"/> part.</param>
/// <param name="Namespace">The <see cref="EventStoreNamespaceName"/> part.</param>
/// <param name="ReadModel">The <see cref="ReadModelIdentifier"/> part.</param>
public record ReadModelReplayManagerGrainKey(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    ReadModelIdentifier ReadModel)
{
    /// <summary>
    /// Implicitly convert from <see cref="ReadModelReplayManagerGrainKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ReadModelReplayManagerGrainKey"/> to convert from.</param>
    public static implicit operator string(ReadModelReplayManagerGrainKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(EventStore, Namespace, ReadModel);

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ReadModelReplayManagerGrainKey"/> instance.</returns>
    public static ReadModelReplayManagerGrainKey Parse(string key) => KeyHelper.Parse<ReadModelReplayManagerGrainKey>(key);
}

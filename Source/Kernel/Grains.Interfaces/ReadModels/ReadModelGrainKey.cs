// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Grains.ReadModels;

/// <summary>
/// Represents the key for a read model.
/// </summary>
/// <param name="Identifier">The <see cref="ReadModelIdentifier"/> part.</param>
/// <param name="EventStore">The <see cref="EventStoreName"/> part.</param>
public record ReadModelGrainKey(ReadModelIdentifier Identifier, EventStoreName EventStore)
{
    /// <summary>
    /// Implicitly convert from <see cref="ReadModelGrainKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ReadModelGrainKey"/> to convert from.</param>
    public static implicit operator string(ReadModelGrainKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(Identifier, EventStore);

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ReadModelGrainKey"/> instance.</returns>
    public static ReadModelGrainKey Parse(string key) => KeyHelper.Parse<ReadModelGrainKey>(key);
}

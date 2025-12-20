// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections;

/// <summary>
/// Represents the compound key for a projection futures.
/// </summary>
/// <param name="ProjectionId">The projection identifier.</param>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The event store namespace.</param>
public record ProjectionFuturesKey(ProjectionId ProjectionId, EventStoreName EventStore, EventStoreNamespaceName Namespace)
{
    /// <summary>
    /// Implicitly convert from <see cref="ProjectionKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ProjectionFuturesKey"/> to convert from.</param>
    public static implicit operator string(ProjectionFuturesKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(ProjectionId, EventStore, Namespace);

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ProjectionFuturesKey"/> instance.</returns>
    public static ProjectionFuturesKey Parse(string key) => KeyHelper.Parse<ProjectionFuturesKey>(key);
}

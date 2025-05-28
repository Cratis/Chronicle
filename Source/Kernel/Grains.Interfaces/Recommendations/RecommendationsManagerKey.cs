// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using EventStoreName = Cratis.Chronicle.Concepts.EventStoreName;
using EventStoreNamespaceName = Cratis.Chronicle.Concepts.EventStoreNamespaceName;

namespace Cratis.Chronicle.Grains.Recommendations;

/// <summary>
/// Represents the key for a <see cref="IRecommendationsManager"/>.
/// </summary>
/// <param name="EventStore">The event store the job is for.</param>
/// <param name="Namespace">The namespace within the event store the job is for.</param>
public record RecommendationsManagerKey(EventStoreName EventStore, EventStoreNamespaceName Namespace)
{
    /// <summary>
    /// Represents an unset key.
    /// </summary>
    public static readonly RecommendationsManagerKey NotSet = new(EventStoreName.NotSet, EventStoreNamespaceName.NotSet);

    /// <summary>
    /// Implicitly convert from string to <see cref="RecommendationsManagerKey"/>.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    public static implicit operator RecommendationsManagerKey(string key) => Parse(key);

    /// <summary>
    /// Implicitly convert from <see cref="RecommendationsManagerKey"/> to string.
    /// </summary>
    /// <param name="key">Key to convert from.</param>
    public static implicit operator string(RecommendationsManagerKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(EventStore, Namespace);

    /// <summary>
    /// Parse a key from a string.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    /// <returns>A <see cref="RecommendationsManagerKey"/> instance.</returns>
    public static RecommendationsManagerKey Parse(string key) => KeyHelper.Parse<RecommendationsManagerKey>(key);
}

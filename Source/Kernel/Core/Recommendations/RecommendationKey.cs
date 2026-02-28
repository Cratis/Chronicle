// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Recommendations;

/// <summary>
/// Represents the key for a recommendation.
/// </summary>
/// <param name="EventStore">The event store the recommendation is for.</param>
/// <param name="Namespace">The namespace the recommendation is for.</param>
public record RecommendationKey(EventStoreName EventStore, EventStoreNamespaceName Namespace)
{
    /// <summary>
    /// Represents an unset key.
    /// </summary>
    public static readonly RecommendationKey NotSet = new(EventStoreName.NotSet, EventStoreNamespaceName.NotSet);

    /// <summary>
    /// Implicitly convert from string to <see cref="RecommendationKey"/>.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    public static implicit operator RecommendationKey(string key) => Parse(key);

    /// <summary>
    /// Implicitly convert from <see cref="RecommendationKey"/> to string.
    /// </summary>
    /// <param name="key">Key to convert from.</param>
    public static implicit operator string(RecommendationKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(EventStore, Namespace);

    /// <summary>
    /// Parse a key from a string.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    /// <returns>A <see cref="RecommendationKey"/> instance.</returns>
    public static RecommendationKey Parse(string key) => KeyHelper.Parse<RecommendationKey>(key);
}

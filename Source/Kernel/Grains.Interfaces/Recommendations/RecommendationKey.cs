// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Recommendations;

/// <summary>
/// Represents the key for a recommendation.
/// </summary>
/// <param name="MicroserviceId">The Microservice the recommendation is for.</param>
/// <param name="TenantId">The tenant the recommendation is for.</param>
public record RecommendationKey(MicroserviceId MicroserviceId, TenantId TenantId)
{
    /// <summary>
    /// Represents an unset key.
    /// </summary>
    public static readonly RecommendationKey NotSet = new(MicroserviceId.Unspecified, TenantId.NotSet);

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
    public override string ToString() => $"{MicroserviceId}+{TenantId}";

    /// <summary>
    /// Parse a key from a string.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    /// <returns>A <see cref="RecommendationKey"/> instance.</returns>
    public static RecommendationKey Parse(string key)
    {
        var elements = key.Split('+');
        var microserviceId = (MicroserviceId)elements[0];
        var tenantId = (TenantId)elements[1];
        return new(microserviceId, tenantId);
    }
}

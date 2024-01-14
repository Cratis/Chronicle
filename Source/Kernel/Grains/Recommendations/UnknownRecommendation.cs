// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Recommendations;

namespace Aksio.Cratis.Kernel.Grains.Recommendations;

/// <summary>
/// Exception that gets thrown when a recommendation does not exist.
/// </summary>
public class UnknownRecommendation : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownRecommendation"/> class.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> the recommendation wasn't found in.</param>
    /// <param name="tenantId">The <see cref="TenantId"/> the recommendation wasn't found for.</param>
    /// <param name="recommendationId">The <see cref="RecommendationId"/> that wasn't found.</param>
    public UnknownRecommendation(
        MicroserviceId microserviceId,
        TenantId tenantId,
        RecommendationId recommendationId) : base($"Unknown recommendation with id {recommendationId} for tenant {tenantId} in microservice {microserviceId}")
    {
    }
}

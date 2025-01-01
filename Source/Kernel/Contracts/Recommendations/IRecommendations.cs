// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Contracts.Recommendations;

/// <summary>
/// Defines the contract for recommendations.
/// </summary>
[Service]
public interface IRecommendations
{
    /// <summary>
    /// Get all recommendations.
    /// </summary>
    /// <param name="request">The request for getting recommendations.</param>
    /// <returns>Collection of recommendations.</returns>
    [Operation]
    Task<IEnumerable<Recommendation>> GetRecommendations(GetRecommendationsRequest request);

    /// <summary>
    /// Observe all recommendations.
    /// </summary>
    /// <param name="request">The request for getting recommendations.</param>
    /// <returns>Collection of recommendations.</returns>
    [Operation]
    IObservable<IEnumerable<Recommendation>> ObserveRecommendations(GetRecommendationsRequest request);
}

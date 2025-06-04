// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Contracts.Recommendations;

/// <summary>
/// Defines the contract for recommendations.
/// </summary>
[Service]
public interface IRecommendations
{
    /// <summary>
    /// Perform a recommendation.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Perform(Perform command, CallContext context = default);

    /// <summary>
    /// Perform a recommendation.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Ignore(Perform command, CallContext context = default);

    /// <summary>
    /// Get all recommendations.
    /// </summary>
    /// <param name="request">The request for getting recommendations.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Collection of recommendations.</returns>
    [Operation]
    Task<IEnumerable<Recommendation>> GetRecommendations(GetRecommendationsRequest request, CallContext context = default);

    /// <summary>
    /// Observe all recommendations.
    /// </summary>
    /// <param name="request">The request for getting recommendations.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Collection of recommendations.</returns>
    [Operation]
    IObservable<IEnumerable<Recommendation>> ObserveRecommendations(GetRecommendationsRequest request, CallContext context = default);
}

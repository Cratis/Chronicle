// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Queries;
using Aksio.Cratis.Kernel.Storage;
using Aksio.Cratis.Kernel.Storage.Recommendations;
using Aksio.Cratis.Recommendations;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Read.Recommendations;

/// <summary>
/// Represents the API for working with recommendations.
/// </summary>
[Route("/api/events/store/{microserviceId}/{tenantId}/recommendations")]
public class Recommendations : ControllerBase
{
    readonly IClusterStorage _clusterStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="Recommendations"/> class.
    /// </summary>
    /// <param name="clusterStorage"><see cref="IClusterStorage"/> for accessing underlying storage.</param>
    public Recommendations(IClusterStorage clusterStorage)
    {
        _clusterStorage = clusterStorage;
    }

    /// <summary>
    /// Get all observers.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the recommendations are for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the recommendations are for.</param>
    /// <returns>Collection of <see cref="RecommendationInformation"/>.</returns>
    [HttpGet]
    public async Task<IEnumerable<RecommendationInformation>> GetRecommendations(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId)
    {
        var recommendations = await _clusterStorage.GetEventStore((string)microserviceId).GetNamespace(tenantId).Recommendations.GeAll();
        return Convert(recommendations);
    }

    /// <summary>
    /// Get and observe all observers.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the recommendations are for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the recommendations are for.</param>
    /// <returns>Client observable of a collection of <see cref="RecommendationInformation"/>.</returns>
    [HttpGet("observe")]
    public Task<ClientObservable<IEnumerable<RecommendationInformation>>> AllRecommendations(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId)
    {
        var clientObservable = new ClientObservable<IEnumerable<RecommendationInformation>>();
        var recommendations = _clusterStorage.GetEventStore((string)microserviceId).GetNamespace(tenantId).Recommendations;
        var observable = recommendations.ObserveRecommendations();
        var subscription = observable.Subscribe(recommendations => clientObservable.OnNext(Convert(recommendations)));
        clientObservable.ClientDisconnected = () =>
        {
            subscription.Dispose();
            if (observable is IDisposable disposableObservable)
            {
                disposableObservable.Dispose();
            }
        };

        return Task.FromResult(clientObservable);
    }

    IEnumerable<RecommendationInformation> Convert(IEnumerable<RecommendationState> recommendations) =>
         recommendations.Select(_ => new RecommendationInformation(_.Id, _.Name, _.Description, _.Type, _.Occurred)).ToArray();
}

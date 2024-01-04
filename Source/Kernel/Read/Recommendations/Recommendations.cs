// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Queries;
using Aksio.Cratis.Kernel.Storage.Recommendations;
using Aksio.Cratis.Recommendations;
using Aksio.DependencyInversion;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Read.Recommendations;

/// <summary>
/// Represents the API for working with recommendations.
/// </summary>
[Route("/api/events/store/{microserviceId}/{tenantId}/recommendations")]
public class Recommendations : ControllerBase
{
    readonly ProviderFor<IRecommendationStorage> _recommendationStorageProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Recommendations"/> class.
    /// </summary>
    /// <param name="recommendationStorageProvider">Provider for <see cref="IRecommendationStorage"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public Recommendations(
        ProviderFor<IRecommendationStorage> recommendationStorageProvider,
        IExecutionContextManager executionContextManager)
    {
        _recommendationStorageProvider = recommendationStorageProvider;
        _executionContextManager = executionContextManager;
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
        _executionContextManager.Establish(tenantId, _executionContextManager.Current.CorrelationId, microserviceId);
        var recommendations = await _recommendationStorageProvider().GetRecommendations();
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
        _executionContextManager.Establish(tenantId, _executionContextManager.Current.CorrelationId, microserviceId);

        var clientObservable = new ClientObservable<IEnumerable<RecommendationInformation>>();
        var observable = _recommendationStorageProvider().ObserveRecommendations();
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

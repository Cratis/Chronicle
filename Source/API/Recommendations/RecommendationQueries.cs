// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Applications.Queries;
using Cratis.Kernel.Storage;
using Cratis.Kernel.Storage.Recommendations;
using Cratis.Recommendations;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.API.Recommendations;

/// <summary>
/// Represents the API for working with recommendations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RecommendationQueries"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
[Route("/api/events/store/{eventStore}/{namespace}/recommendations")]
public class RecommendationQueries(IStorage storage) : ControllerBase
{
    /// <summary>
    /// Get all observers for an event store and namespace.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the recommendations are for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the recommendations are for.</param>
    /// <returns>Collection of <see cref="RecommendationInformation"/>.</returns>
    [HttpGet]
    public async Task<IEnumerable<RecommendationInformation>> GetRecommendations(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace)
    {
        var recommendations = await storage.GetEventStore(eventStore).GetNamespace(@namespace).Recommendations.GeAll();
        return Convert(recommendations);
    }

    /// <summary>
    /// Get and observe all observers for an event store and namespace.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the recommendations are for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the recommendations are for.</param>
    /// <returns>Client observable of a collection of <see cref="RecommendationInformation"/>.</returns>
    [HttpGet("observe")]
    public Task<ClientObservable<IEnumerable<RecommendationInformation>>> AllRecommendations(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace)
    {
        var recommendations = storage.GetEventStore(eventStore).GetNamespace(@namespace).Recommendations;
        var observable = recommendations.ObserveRecommendations();
        new Subject<IEnumerable<RecommendationInformation>>();
        var clientObservable = new ClientObservable<IEnumerable<RecommendationInformation>>(observable, new Microsoft.AspNetCore.Mvc.JsonOptions());
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

    RecommendationInformation[] Convert(IEnumerable<RecommendationState> recommendations) =>
         recommendations.Select(_ => new RecommendationInformation(_.Id, _.Name, _.Description, _.Type, _.Occurred)).ToArray();
}

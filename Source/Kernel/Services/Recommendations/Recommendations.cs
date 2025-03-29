// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.Recommendations;
using Cratis.Chronicle.Grains.Recommendations;
using Cratis.Chronicle.Reactive;
using Cratis.Chronicle.Storage;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Recommendations;

/// <summary>
/// Represents an implementation of <see cref="IRecommendations"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
/// <param name="storage"><see cref="IStorage"/> for recommendations.</param>
public class Recommendations(IGrainFactory grainFactory, IStorage storage) : IRecommendations
{
    /// <inheritdoc/>
    public Task Perform(Perform command, CallContext context = default) =>
        GetRecommendationsManager(command.EventStore, command.Namespace).Perform(command.RecommendationId);

    /// <inheritdoc/>
    public Task Ignore(Perform command, CallContext context = default) =>
        GetRecommendationsManager(command.EventStore, command.Namespace).Ignore(command.RecommendationId);

    /// <inheritdoc/>
    public async Task<IEnumerable<Recommendation>> GetRecommendations(GetRecommendationsRequest request, CallContext context = default)
    {
        var recommendations = await storage.GetEventStore(request.EventStore).GetNamespace(request.Namespace).Recommendations.GetAll();
        return recommendations.ToContract();
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<Recommendation>> ObserveRecommendations(GetRecommendationsRequest request, CallContext context = default) =>
        storage
            .GetEventStore(request.EventStore)
            .GetNamespace(request.Namespace).Recommendations
            .ObserveRecommendations()
            .CompletedBy(context.CancellationToken)
            .Select(_ => _.ToContract());

    IRecommendationsManager GetRecommendationsManager(EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        grainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(eventStore, @namespace));
}

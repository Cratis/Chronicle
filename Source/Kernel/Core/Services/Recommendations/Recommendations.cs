// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Recommendations;
using Cratis.Chronicle.Recommendations;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Recommendations;

/// <summary>
/// Represents an implementation of <see cref="IRecommendations"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
/// <param name="storage"><see cref="IStorage"/> for recommendations.</param>
internal sealed class Recommendations(IGrainFactory grainFactory, IStorage storage) : IRecommendations
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

    IRecommendationsManager GetRecommendationsManager(Concepts.EventStoreName eventStore, Concepts.EventStoreNamespaceName @namespace) =>
        grainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(eventStore, @namespace));
}

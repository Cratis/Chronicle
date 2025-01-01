// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Recommendations;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services.Recommendations;

/// <summary>
/// Represents an implementation of <see cref="IRecommendations"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for recommendations.</param>
public class Recommendations(IStorage storage) : IRecommendations
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Recommendation>> GetRecommendations(GetRecommendationsRequest request)
    {
        var recommendations = await storage.GetEventStore(request.EventStore).GetNamespace(request.Namespace).Recommendations.GetAll();
        return recommendations.ToContract();
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<Recommendation>> ObserveRecommendations(GetRecommendationsRequest request) =>
        storage.GetEventStore(request.EventStore).GetNamespace(request.Namespace).Recommendations.ObserveRecommendations().Select(_ => _.ToContract());
}

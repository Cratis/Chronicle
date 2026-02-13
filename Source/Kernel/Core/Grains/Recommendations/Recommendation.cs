// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Storage.Recommendations;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Recommendations;

/// <summary>
/// Represents an implementation of <see cref="IRecommendation{TRequest}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request for the recommendation.</typeparam>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Recommendations)]
public class Recommendation<TRequest> : Grain<RecommendationState>, IRecommendation<TRequest>
    where TRequest : class, IRecommendationRequest
{
    /// <inheritdoc/>
    public async Task Initialize(
        RecommendationDescription description,
        TRequest request)
    {
        State.Name = GetType().Name;
        State.Description = description;
        State.Type = this.GetGrainType();
        State.Request = request;
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task Perform()
    {
        var request = (State.Request as TRequest)!;
        await OnPerform(request);
        await ClearStateAsync();
        DeactivateOnIdle();
    }

    /// <inheritdoc/>
    public async Task Ignore()
    {
        await ClearStateAsync();
        DeactivateOnIdle();
    }

    /// <summary>
    /// THe method that gets called when the recommendation is performed.
    /// </summary>
    /// <param name="request">The request for the recommendation.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnPerform(TRequest request) => Task.CompletedTask;
}

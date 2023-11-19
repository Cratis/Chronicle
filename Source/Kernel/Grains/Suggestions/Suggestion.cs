// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Providers;

namespace Aksio.Cratis.Kernel.Grains.Suggestions;

/// <summary>
/// Represents an implementation of <see cref="ISuggestion{TRequest}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request for the suggestion.</typeparam>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Suggestions)]
public class Suggestion<TRequest> : Grain<SuggestionState>, ISuggestion<TRequest>
    where TRequest : class
{
    /// <inheritdoc/>
    public async Task Initialize(
        SuggestionDescription description,
        TRequest request)
    {
        var requestType = request.GetType();
        State.Name = requestType.Name;
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
    /// THe method that gets called when the suggestion is performed.
    /// </summary>
    /// <param name="request">The request for the suggestion.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnPerform(TRequest request) => Task.CompletedTask;
}

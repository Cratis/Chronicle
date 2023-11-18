// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Suggestions;

/// <summary>
/// Represents an implementation of <see cref="ISuggestion{TRequest}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request for the suggestion.</typeparam>
public class Suggestion<TRequest> : Grain<SuggestionState>, ISuggestion<TRequest>
    where TRequest : class
{
    /// <inheritdoc/>
    public async Task Perform()
    {
        var request = (State.Request as TRequest)!;
        await OnPerform(request);
        await ClearStateAsync();
    }

    /// <summary>
    /// THe method that gets called when the suggestion is performed.
    /// </summary>
    /// <param name="request">The request for the suggestion.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnPerform(TRequest request) => Task.CompletedTask;
}

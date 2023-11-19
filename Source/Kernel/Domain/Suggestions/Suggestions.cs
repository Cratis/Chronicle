// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Suggestions;
using Aksio.Cratis.Kernel.Suggestions;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Domain.Suggestions;

/// <summary>
/// Represents the API for working with suggestions.
/// </summary>
[Route("/api/events/store/{microserviceId}/{tenantId}/suggestions")]
public class Suggestions : ControllerBase
{
    readonly IGrainFactory _grainFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="Suggestions"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    public Suggestions(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    /// <summary>
    /// Perform a suggestion.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> for the suggestion.</param>
    /// <param name="tenantId">The <see cref="TenantId"/> for the suggestion.</param>
    /// <param name="suggestionId">The <see cref="SuggestionId"/> of the suggestion to perform.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{suggestionId}/perform")]
    public async Task Perform(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId,
        [FromRoute] SuggestionId suggestionId)
    {
        await GetSuggestionsManager(microserviceId, tenantId).Perform(suggestionId);
    }

    /// <summary>
    /// Ignore a suggestion.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> for the suggestion.</param>
    /// <param name="tenantId">The <see cref="TenantId"/> for the suggestion.</param>
    /// <param name="suggestionId">The <see cref="SuggestionId"/> of the suggestion to ignore.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{suggestionId}/ignore")]
    public async Task Ignore(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId,
        [FromRoute] SuggestionId suggestionId)
    {
        await GetSuggestionsManager(microserviceId, tenantId).Ignore(suggestionId);
    }

    ISuggestionsManager GetSuggestionsManager(MicroserviceId microserviceId, TenantId tenantId) =>
        _grainFactory.GetGrain<ISuggestionsManager>(0, new SuggestionsManagerKey(microserviceId, tenantId));
}

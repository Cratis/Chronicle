// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Suggestions;

namespace Aksio.Cratis.Kernel.Grains.Suggestions;

/// <summary>
/// Represents an implementation of <see cref="ISuggestionsManager"/> that has a result.
/// </summary>
public class SuggestionsManager : Grain, ISuggestionsManager
{
    /// <inheritdoc/>
    public async Task<SuggestionId> Add<TSuggestion, TRequest>(TRequest request)
        where TSuggestion : ISuggestion<TRequest>
        where TRequest : class
    {
        this.GetPrimaryKey(out var keyAsString);
        var key = (SuggestionsManagerKey)keyAsString;
        var id = SuggestionId.New();
        var suggestion = GrainFactory.GetGrain<TSuggestion>(id, keyExtension: new SuggestionKey(key.MicroserviceId, key.TenantId));
        await suggestion.Initialize(request);
        return id;
    }
}

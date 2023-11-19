// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Persistence.Suggestions;
using Aksio.Cratis.Kernel.Suggestions;
using Aksio.DependencyInversion;

namespace Aksio.Cratis.Kernel.Grains.Suggestions;

/// <summary>
/// Represents an implementation of <see cref="ISuggestionsManager"/> that has a result.
/// </summary>
public class SuggestionsManager : Grain, ISuggestionsManager
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<ISuggestionStorage> _storageProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuggestionsManager"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="storageProvider">Provider for <see cref="ISuggestionStorage"/>.</param>
    public SuggestionsManager(
        IExecutionContextManager executionContextManager,
        ProviderFor<ISuggestionStorage> storageProvider)
    {
        _executionContextManager = executionContextManager;
        _storageProvider = storageProvider;
    }

    /// <inheritdoc/>
    public async Task<SuggestionId> Add<TSuggestion, TRequest>(SuggestionDescription description, TRequest request)
        where TSuggestion : ISuggestion<TRequest>
        where TRequest : class
    {
        var id = SuggestionId.New();
        var suggestion = GrainFactory.GetGrain<TSuggestion>(id, keyExtension: GetSuggestionKey());
        await suggestion.Initialize(description, request);
        return id;
    }

    /// <inheritdoc/>
    public async Task Ignore(SuggestionId suggestionId)
    {
        var suggestion = await GetGrainFor(suggestionId);
        await suggestion.Ignore();
    }

    /// <inheritdoc/>
    public async Task Perform(SuggestionId suggestionId)
    {
        var suggestion = await GetGrainFor(suggestionId);
        await suggestion.Perform();
    }

    async Task<ISuggestion> GetGrainFor(SuggestionId suggestionId)
    {
        var key = GetSuggestionKey();
        _executionContextManager.Establish(key.TenantId, _executionContextManager.Current.CorrelationId, key.MicroserviceId);
        var suggestionState = await _storageProvider().Get(suggestionId) ?? throw new UnknownSuggestion(key.MicroserviceId, key.TenantId, suggestionId);
        var suggestionType = (Type)suggestionState.Type;
        return GrainFactory.GetGrain(suggestionType, suggestionId, keyExtension: GetSuggestionKey()).AsReference<ISuggestion>();
    }

    SuggestionKey GetSuggestionKey()
    {
        this.GetPrimaryKey(out var keyAsString);
        var key = (SuggestionsManagerKey)keyAsString;
        return new SuggestionKey(key.MicroserviceId, key.TenantId);
    }
}

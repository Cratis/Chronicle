// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Suggestions;

namespace Aksio.Cratis.Kernel.Grains.Suggestions;

/// <summary>
/// Defines a system that manages suggestions.
/// </summary>
public interface ISuggestionsManager : IGrainWithIntegerCompoundKey
{
    /// <summary>
    /// Add a suggestion.
    /// </summary>
    /// <param name="description">The description of the suggestion.</param>
    /// <param name="request">The request for the suggestion.</param>
    /// <typeparam name="TSuggestion">Type of suggestion to add.</typeparam>
    /// <typeparam name="TRequest">Type of request for the suggestion.</typeparam>
    /// <returns>The <see cref="SuggestionId"/> for the added suggestion.</returns>
    Task<SuggestionId> Add<TSuggestion, TRequest>(SuggestionDescription description, TRequest request)
        where TSuggestion : ISuggestion<TRequest>
        where TRequest : class;

    /// <summary>
    /// Perform a suggestion.
    /// </summary>
    /// <param name="suggestionId">The <see cref="SuggestionId"/> to perform.</param>
    /// <returns>Awaitable task.</returns>
    Task Perform(SuggestionId suggestionId);

    /// <summary>
    /// Ignore a suggestion.
    /// </summary>
    /// <param name="suggestionId">The <see cref="SuggestionId"/> to ignore.</param>
    /// <returns>Awaitable task.</returns>
    Task Ignore(SuggestionId suggestionId);
}

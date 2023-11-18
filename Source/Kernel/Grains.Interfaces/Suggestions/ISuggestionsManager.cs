// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Suggestions;

namespace Aksio.Cratis.Kernel.Grains.Suggestions;

/// <summary>
/// Defines a system that manages suggestions.
/// </summary>
public interface ISuggestionsManager : IGrainWithIntegerKey
{
    /// <summary>
    /// Add a suggestion.
    /// </summary>
    /// <param name="request">The request for the suggestion.</param>
    /// <typeparam name="TSuggestion">Type of suggestion to add.</typeparam>
    /// <typeparam name="TRequest">Type of request for the suggestion.</typeparam>
    /// <returns>The <see cref="SuggestionId"/> for the added suggestion.</returns>
    Task<SuggestionId> Add<TSuggestion, TRequest>(TRequest request)
        where TSuggestion : ISuggestion<TRequest>
        where TRequest : class;
}

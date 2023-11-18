// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Suggestions;

namespace Aksio.Cratis.Kernel.Grains.Suggestions;

/// <summary>
/// Represents an implementation of <see cref="ISuggestionsManager"/> that has a result.
/// </summary>
public class SuggestionManager : Grain, ISuggestionsManager
{
    /// <inheritdoc/>
    public Task<SuggestionId> Add<TSuggestion, TRequest>(TRequest request)
        where TSuggestion : ISuggestion<TRequest>
        where TRequest : class => throw new NotImplementedException();
}

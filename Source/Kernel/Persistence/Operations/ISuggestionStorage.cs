// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Kernel.Grains.Suggestions;
using Aksio.Cratis.Kernel.Suggestions;

namespace Aksio.Cratis.Kernel.Persistence.Suggestions;

/// <summary>
/// Defines the storage for <see cref="SuggestionState"/>.
/// </summary>
public interface ISuggestionStorage
{
    /// <summary>
    /// Get a suggestion by its <see cref="SuggestionId"/>.
    /// </summary>
    /// <param name="suggestionId">The <see cref="SuggestionId"/> to get for.</param>
    /// <returns>An <see cref="SuggestionState"/> if it was found, null if not.</returns>
    Task<SuggestionState?> Get(SuggestionId suggestionId);

    /// <summary>
    /// Save a suggestion.
    /// </summary>
    /// <param name="suggestionId">The <see cref="SuggestionId"/> to save for.</param>
    /// <param name="suggestionState">The <see cref="SuggestionState"/> to save.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(SuggestionId suggestionId, SuggestionState suggestionState);

    /// <summary>
    /// Remove a suggestion.
    /// </summary>
    /// <param name="suggestionId">The <see cref="SuggestionId"/> of the suggestion to remove.</param>
    /// <returns>Awaitable task.</returns>
    Task Remove(SuggestionId suggestionId);

    /// <summary>
    /// Get all suggestions.
    /// </summary>
    /// <returns>A collection of <see cref="SuggestionState"/>.</returns>
    Task<IImmutableList<SuggestionState>> GetSuggestions();

    /// <summary>
    /// Observe all suggestions.
    /// </summary>
    /// <returns>An observable of collection of suggestions.</returns>
    IObservable<IEnumerable<SuggestionState>> ObserveSuggestions();
}

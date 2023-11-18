// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Suggestions;

namespace Aksio.Cratis.Kernel.Grains.Suggestions;

/// <summary>
/// Holds the state of a <see cref="ISuggestion{TRequest}"/>.
/// </summary>
public class SuggestionState
{
    /// <summary>
    /// Gets or sets the <see cref="SuggestionId"/>.
    /// </summary>
    public SuggestionId Id { get; set; } = SuggestionId.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="SuggestionName"/>.
    /// </summary>
    public SuggestionName Name { get; set; } = SuggestionName.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="SuggestionDescription"/>.
    /// </summary>
    public SuggestionDescription Description { get; set; } = SuggestionDescription.NotSet;

    /// <summary>
    /// Gets or sets the <see cref="SuggestionType"/>.
    /// </summary>
    public SuggestionType Type { get; set; } = SuggestionType.NotSet;

    /// <summary>
    /// Gets or sets when the suggestion occurred.
    /// </summary>
    public DateTimeOffset Occurred { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the request associated with the suggestion.
    /// </summary>
    public object Request { get; set; } = default!;
}

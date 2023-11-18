// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Suggestions;

namespace Aksio.Cratis.Kernel.Suggestions;

/// <summary>
/// Represents information about an suggestion.
/// </summary>
/// <param name="Id">The unique identifier of the suggestion.</param>
/// <param name="Name">The name of the suggestion.</param>
/// <param name="Description">The details of the suggestion.</param>
/// <param name="Type">The type of the suggestion.</param>
/// <param name="Occurred">When the suggestion occurred.</param>
public record SuggestionInformation(
    SuggestionId Id,
    SuggestionName Name,
    SuggestionDescription Description,
    SuggestionType Type,
    DateTimeOffset Occurred);

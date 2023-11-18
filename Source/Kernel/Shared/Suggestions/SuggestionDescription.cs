// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Suggestions;

/// <summary>
/// Represents the description for a suggestion.
/// </summary>
/// <param name="Value">The actual value.</param>
public record SuggestionDescription(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The name of a job that is not set.
    /// </summary>
    public static readonly SuggestionDescription NotSet = "[Not set]";

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="SuggestionDescription"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator SuggestionDescription(string value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="SuggestionDescription"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"><see cref="SuggestionDescription"/> to convert from.</param>
    public static implicit operator string(SuggestionDescription value) => value.Value;
}

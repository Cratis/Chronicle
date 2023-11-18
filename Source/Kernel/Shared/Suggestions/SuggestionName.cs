// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Suggestions;

/// <summary>
/// Represents the name of a suggestion.
/// </summary>
/// <param name="Value">The actual value.</param>
public record SuggestionName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The name of a job that is not set.
    /// </summary>
    public static readonly SuggestionName NotSet = "[Not set]";

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="SuggestionName"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator SuggestionName(string value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="SuggestionName"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"><see cref="SuggestionName"/> to convert from.</param>
    public static implicit operator string(SuggestionName value) => value.Value;
}

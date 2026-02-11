// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Settings;

/// <summary>
/// Represents a language model identifier.
/// </summary>
/// <param name="Value">The language model name.</param>
public record LanguageModel(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets a <see cref="LanguageModel"/> representing an empty or not set model.
    /// </summary>
    public static readonly LanguageModel NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts from <see cref="string"/> to <see cref="LanguageModel"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator LanguageModel(string value) => new(value);

    /// <summary>
    /// Implicitly converts from <see cref="LanguageModel"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="model">The language model to convert.</param>
    public static implicit operator string(LanguageModel model) => model.Value;
}

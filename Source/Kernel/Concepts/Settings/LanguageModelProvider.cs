// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Concepts.Settings;

/// <summary>
/// Represents the language model provider configuration.
/// </summary>
[JsonConverter(typeof(LanguageModelProviderJsonConverter))]
public sealed class LanguageModelProvider : OneOf.OneOfBase<OpenAIProvider, OneOf.Types.None>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageModelProvider"/> class.
    /// </summary>
    /// <param name="input">The input value.</param>
    LanguageModelProvider(OneOf.OneOf<OpenAIProvider, OneOf.Types.None> input) : base(input)
    {
    }

    /// <summary>
    /// Gets a <see cref="LanguageModelProvider"/> representing no provider configured.
    /// </summary>
    public static LanguageModelProvider None => new(OneOf.OneOf<OpenAIProvider, OneOf.Types.None>.FromT1(default));

    /// <summary>
    /// Implicitly converts from <see cref="OpenAIProvider"/> to <see cref="LanguageModelProvider"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator LanguageModelProvider(OpenAIProvider value) => new(value);

    /// <summary>
    /// Implicitly converts from <see cref="OneOf.Types.None"/> to <see cref="LanguageModelProvider"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
#pragma warning disable IDE0060 // Remove unused parameter
    public static implicit operator LanguageModelProvider(OneOf.Types.None value) => None;
#pragma warning restore IDE0060 // Remove unused parameter
}

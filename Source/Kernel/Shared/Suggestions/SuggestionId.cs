// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Suggestions;

/// <summary>
/// Represents the unique identifier of an suggestion.
/// </summary>
/// <param name="Value">Inner value.</param>
public record SuggestionId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Represents the "not set" <see cref="SuggestionId"/>.
    /// </summary>
    public static readonly SuggestionId NotSet = Guid.Empty;

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="SuggestionId"/>.
    /// </summary>
    /// <param name="id"><see cref="Guid"/> representation.</param>
    public static implicit operator SuggestionId(Guid id) => new(id);

    /// <summary>
    /// Create a new <see cref="SuggestionId"/>.
    /// </summary>
    /// <returns>A new <see cref="SuggestionId"/>.</returns>
    public static SuggestionId New() => new(Guid.NewGuid());
}

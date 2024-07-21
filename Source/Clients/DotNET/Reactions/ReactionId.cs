// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactions;

/// <summary>
/// Concept that represents the unique identifier of an reaction.
/// </summary>
/// <param name="Value">Actual value.</param>
public record ReactionId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the representation of an unspecified <see cref="ReactionId"/>.
    /// </summary>
    public static readonly ReactionId Unspecified = new("[unspecified]");

    /// <summary>
    /// Implicitly convert from a string to <see cref="ReactionId"/>.
    /// </summary>
    /// <param name="id">String to convert from.</param>
    public static implicit operator ReactionId(string id) => new(id);
}

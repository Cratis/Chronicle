// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.ReadModels;

/// <summary>
/// Represents the display name of a read model.
/// </summary>
/// <param name="Value">The inner value.</param>
public record ReadModelDisplayName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets a value representing an unset display name.
    /// </summary>
    public static readonly ReadModelDisplayName NotSet = "NotSet";

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="ReadModelDisplayName"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> to convert from.</param>
    public static implicit operator ReadModelDisplayName(string value) => new(value);
}

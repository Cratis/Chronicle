// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.ReadModels;

/// <summary>
/// Represents the friendly display name of a model.
/// </summary>
/// <param name="Value">The inner value.</param>
public record ReadModelName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets a value representing an unset model name.
    /// </summary>
    public static readonly ReadModelName NotSet = "NotSet";

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="ReadModelName"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> to convert from.</param>
    public static implicit operator ReadModelName(string value) => new(value);
}

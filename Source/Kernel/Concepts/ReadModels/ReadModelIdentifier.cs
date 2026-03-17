// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.ReadModels;

/// <summary>
/// Represents the unique identifier of a read model.
/// </summary>
/// <param name="Value">The inner value.</param>
public record ReadModelIdentifier(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets a value representing an unset model identifier.
    /// </summary>
    public static readonly ReadModelIdentifier NotSet = "NotSet";

    /// <summary>
    /// Gets a value representing an inferred model identifier when no explicit read model is specified in the projection declaration.
    /// </summary>
    public static readonly ReadModelIdentifier Inferred = "$inferred";

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="ReadModelIdentifier"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> to convert from.</param>
    public static implicit operator ReadModelIdentifier(string value) => new(value);
}

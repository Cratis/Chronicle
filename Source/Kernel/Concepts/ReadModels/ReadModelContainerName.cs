// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.ReadModels;

/// <summary>
/// Represents the storage container name of a read model (collection, table, etc.).
/// </summary>
/// <param name="Value">The inner value.</param>
public record ReadModelContainerName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets a value representing an unset container name.
    /// </summary>
    public static readonly ReadModelContainerName NotSet = "NotSet";

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="ReadModelContainerName"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> to convert from.</param>
    public static implicit operator ReadModelContainerName(string value) => new(value);
}

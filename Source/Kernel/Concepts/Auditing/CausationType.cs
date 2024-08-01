// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing;

/// <summary>
/// Represents a causation type.
/// </summary>
/// <param name="Name">String representing the name of the type.</param>
public record CausationType(string Name) : ConceptAs<string>(Name)
{
    /// <summary>
    /// Represents the root causation type.
    /// </summary>
    public static readonly CausationType Root = new("Root");

    /// <summary>
    /// Represents the unknown causation type.
    /// </summary>
    public static readonly CausationType Unknown = new("Unknown");

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="CausationType"/>.
    /// </summary>
    /// <param name="name">Name of causation type.</param>
    public static implicit operator CausationType(string name) => new(name);

    /// <summary>
    /// Implicitly convert from <see cref="CausationType"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="causationType">Causation type to convert from.</param>
    public static implicit operator string(CausationType causationType) => causationType.Name;
}

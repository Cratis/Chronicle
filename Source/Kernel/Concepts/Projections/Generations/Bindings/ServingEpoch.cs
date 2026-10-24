// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

/// <summary>
/// Represents the serving epoch.
/// </summary>
/// <param name="Value">The value.</param>
public record ServingEpoch(uint Value) : ConceptAs<uint>(Value)
{
    /// <summary>
    /// Represents the unset.
    /// </summary>
    public static readonly ServingEpoch Unset = new(0);

    /// <summary>
    /// Represents the initial.
    /// </summary>
    public static readonly ServingEpoch Initial = new(1);

    /// <summary>
    /// Advances the serving epoch without wrapping.
    /// </summary>
    /// <returns>The next serving epoch.</returns>
    public ServingEpoch Next() => new(checked(Value + 1));
}

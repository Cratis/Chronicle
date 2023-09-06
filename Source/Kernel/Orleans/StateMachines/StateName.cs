// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Orleans.StateMachines;

/// <summary>
/// Represents the name of a state.
/// </summary>
/// <param name="Value">The actual name.</param>
public record StateName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="StateName"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator StateName(string value) => new(value);
}

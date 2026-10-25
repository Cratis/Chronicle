// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Represents a monotonic number assigned by a deployment pipeline, such as a CI run number or timestamp; 0 means none was sent.
/// </summary>
/// <param name="Value">The generation.</param>
public record DeploymentGeneration(ulong Value) : ConceptAs<ulong>(Value)
{
    /// <summary>
    /// The value representing no generation sent.
    /// </summary>
    public static readonly DeploymentGeneration NotSet = new(0UL);

    /// <summary>
    /// Gets whether a generation was sent.
    /// </summary>
    public bool IsSet => Value != NotSet.Value;

    /// <summary>
    /// Implicitly converts a number to a generation.
    /// </summary>
    /// <param name="value">The ulong value.</param>
    public static implicit operator DeploymentGeneration(ulong value) => new(value);
}

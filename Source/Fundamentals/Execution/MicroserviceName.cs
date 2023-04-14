// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Concepts;

namespace Aksio.Cratis.Execution;

/// <summary>
/// Represents the concept of the microservice name.
/// </summary>
/// <param name="Value">Actual value.</param>
public record MicroserviceName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The value when microservice name is not specified.
    /// </summary>
    public static readonly MicroserviceName Unspecified = new("[Not Set]");

    /// <summary>
    /// Implicitly convert from a string to <see cref="MicroserviceName"/>.
    /// </summary>
    /// <param name="name">Name of microservice.</param>
    /// <returns><see cref="MicroserviceName"/> representation of the name.</returns>
    public static implicit operator MicroserviceName(string name) => new(name);
}

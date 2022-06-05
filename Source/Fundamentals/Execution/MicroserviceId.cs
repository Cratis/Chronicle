// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Concepts;

namespace Aksio.Cratis.Execution;

/// <summary>
/// Represents the concept of the microservice identifier.
/// </summary>
/// <param name="Value">Actual value.</param>
public record MicroserviceId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Represents a value that identifies the Kernel as a microservice.
    /// </summary>
    public static readonly MicroserviceId Kernel = Guid.Parse("12c737d2-e816-46f4-96fd-67fc1bf71086");

    /// <summary>
    /// The value when microservice identifier is not specified.
    /// </summary>
    public static readonly MicroserviceId Unspecified = new(Guid.Empty);

    /// <summary>
    /// Implicitly convert from a string representation of a <see cref="Guid"/> to <see cref="MicroserviceId"/>.
    /// </summary>
    /// <param name="id">String representation of a <see cref="Guid"/> to convert from.</param>
    public static implicit operator MicroserviceId(string id) => new(Guid.Parse(id));

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="MicroserviceId"/>.
    /// </summary>
    /// <param name="id"><see cref="Guid"/> to convert from.</param>
    public static implicit operator MicroserviceId(Guid id) => new(id);

    /// <summary>
    /// Implicitly convert from <see cref="MicroserviceId"/> to string.
    /// </summary>
    /// <param name="id"><see cref="MicroserviceId"/> to convert from.</param>
    public static implicit operator string(MicroserviceId id) => id.ToString();
}

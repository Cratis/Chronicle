// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Jobs;

/// <summary>
/// Represents type of a Job.
/// </summary>
/// <param name="Value">String representation of the job type.</param>
/// <remarks>
/// The expected format is <c>Namespace.Type</c>.
/// </remarks>
public record JobType(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The <see cref="JobType"/> for when it is not set.
    /// </summary>
    public static readonly JobType NotSet = new("Undefined");

    /// <summary>
    /// Implicitly convert from <see cref="Type"/> to <see cref="JobType"/>.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to convert from.</param>
    public static implicit operator JobType(Type type) => new(type.Name);
}
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents type of a job step.
/// </summary>
/// <param name="Value">String representation of the job type.</param>
/// <remarks>
/// The expected format is <c>Namespace.Type</c>.
/// </remarks>
public record JobStepType(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The <see cref="JobStepType"/> for when it is not set.
    /// </summary>
    public static readonly JobStepType NotSet = new("Undefined");

    public static implicit operator JobStepType(string value) => new(value);
}

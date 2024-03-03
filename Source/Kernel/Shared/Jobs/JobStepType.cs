// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Jobs;

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

    /// <summary>
    /// Implicitly convert from <see cref="Type"/> to <see cref="JobType"/>.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to convert from.</param>
    public static implicit operator JobStepType(Type type) => new(type.AssemblyQualifiedName ?? type.Name);

    /// <summary>
    /// Implicitly convert from <see cref="JobType"/> to <see cref="Type"/>.
    /// </summary>
    /// <param name="type"><see cref="JobType"/> to convert from.</param>
    public static implicit operator Type(JobStepType type) => Type.GetType(type.Value) ?? throw new UnknownClrTypeForJobStepType(type);
}

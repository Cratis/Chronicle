// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Jobs;

/// <summary>
/// Represents the identifier of a job step.
/// </summary>
/// <param name="Value">Actual value.</param>
public record JobStepId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Gets the identifier for an unknown job step.
    /// </summary>
    public static readonly JobStepId Unknown = Guid.Empty;

    /// <summary>
    /// Implicitly convert from <see cref="Guid"/> to <see cref="JobStepId"/>.
    /// </summary>
    /// <param name="value"><see cref="Guid"/> value to convert.</param>
    public static implicit operator JobStepId(Guid value) => new(value);

    /// <summary>
    /// Create a new unique <see cref="JobStepId"/>.
    /// </summary>
    /// <returns>A new <see cref="JobStepId"/>.</returns>
    public static JobStepId New() => new(Guid.NewGuid());
}

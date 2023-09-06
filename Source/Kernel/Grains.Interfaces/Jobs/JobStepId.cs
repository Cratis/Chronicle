// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents the identifier of a job step.
/// </summary>
/// <param name="Value">Actual value.</param>
public record JobStepId(int Value) : ConceptAs<int>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="int"/> to <see cref="JobStepId"/>.
    /// </summary>
    /// <param name="value"><see cref="int"/> value to convert.</param>
    public static implicit operator JobStepId(int value) => new(value);
}

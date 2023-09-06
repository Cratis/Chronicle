// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents the progress percentage of a step.
/// </summary>
/// <param name="Value">Inner value.</param>
public record StepPercentage(int Value) : ConceptAs<int>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="int"/> to <see cref="StepPercentage"/>.
    /// </summary>
    /// <param name="value">Integer to convert from.</param>
    public static implicit operator StepPercentage(int value) => new(value);
}

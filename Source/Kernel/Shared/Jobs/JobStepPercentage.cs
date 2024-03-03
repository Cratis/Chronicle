// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Jobs;

/// <summary>
/// Represents the progress percentage of a step.
/// </summary>
/// <param name="Value">Inner value.</param>
public record JobStepPercentage(int Value) : ConceptAs<int>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="int"/> to <see cref="JobStepPercentage"/>.
    /// </summary>
    /// <param name="value">Integer to convert from.</param>
    public static implicit operator JobStepPercentage(int value) => new(value);
}

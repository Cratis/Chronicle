// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Jobs;

/// <summary>
/// Represents the name of a job step.
/// </summary>
/// <param name="Value">The actual value.</param>
public record JobStepName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The name of a job step that is not set.
    /// </summary>
    public static readonly JobStepName NotSet = "[Not set]";

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="JobStepName"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator JobStepName(string value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="JobStepName"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"><see cref="JobStepName"/> to convert from.</param>
    public static implicit operator string(JobStepName value) => value.Value;
}

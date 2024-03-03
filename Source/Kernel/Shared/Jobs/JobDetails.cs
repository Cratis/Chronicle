// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Jobs;

/// <summary>
/// Represents details about a job.
/// </summary>
/// <param name="Value">The actual value.</param>
public record JobDetails(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The name of a job that is not set.
    /// </summary>
    public static readonly JobDetails NotSet = "[Not set]";

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="JobDetails"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator JobDetails(string value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="JobDetails"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"><see cref="JobDetails"/> to convert from.</param>
    public static implicit operator string(JobDetails value) => value.Value;
}

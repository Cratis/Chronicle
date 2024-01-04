// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Jobs;

/// <summary>
/// Represents the name of a job.
/// </summary>
/// <param name="Value">The actual value.</param>
public record JobName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The name of a job that is not set.
    /// </summary>
    public static readonly JobName NotSet = "[Not set]";

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="JobName"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator JobName(string value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="JobName"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"><see cref="JobName"/> to convert from.</param>
    public static implicit operator string(JobName value) => value.Value;
}

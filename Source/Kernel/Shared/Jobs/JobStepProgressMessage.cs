// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Jobs;

/// <summary>
/// Represents the message of a job progress.
/// </summary>
/// <param name="Value">The inner value.</param>
public record JobStepProgressMessage(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an empty <see cref="JobStepProgressMessage"/>.
    /// </summary>
    public static readonly JobStepProgressMessage None = string.Empty;

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="JobStepProgressMessage"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> value to convert.</param>
    public static implicit operator JobStepProgressMessage(string value) => new(value);
}

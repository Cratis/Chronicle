// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Jobs;

/// <summary>
/// Represents the message of a job progress.
/// </summary>
/// <param name="Value">The inner value.</param>
public record JobProgressMessage(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents an empty <see cref="JobProgressMessage"/>.
    /// </summary>
    public static readonly JobProgressMessage None = string.Empty;

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="JobProgressMessage"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> value to convert.</param>
    public static implicit operator JobProgressMessage(string value) => new(value);
}

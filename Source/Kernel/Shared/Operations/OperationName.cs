// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Operations;

/// <summary>
/// Represents the name of a task.
/// </summary>
/// <param name="Value">The actual value.</param>
public record OperationName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The name of a job that is not set.
    /// </summary>
    public static readonly OperationName NotSet = "[Not set]";

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="OperationName"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator OperationName(string value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="OperationName"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"><see cref="OperationName"/> to convert from.</param>
    public static implicit operator string(OperationName value) => value.Value;
}

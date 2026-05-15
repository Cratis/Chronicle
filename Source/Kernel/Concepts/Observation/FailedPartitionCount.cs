// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Represents the count of failed partitions for an observer.
/// </summary>
/// <param name="Value">The underlying count value.</param>
public record FailedPartitionCount(int Value) : ConceptAs<int>(Value)
{
    /// <summary>
    /// Represents an unset <see cref="FailedPartitionCount"/>.
    /// </summary>
    public static readonly FailedPartitionCount NotSet = 0;

    /// <summary>
    /// Represents a zero <see cref="FailedPartitionCount"/>.
    /// </summary>
    public static readonly FailedPartitionCount Zero = 0;

    /// <summary>
    /// Implicitly converts an <see cref="int"/> to a <see cref="FailedPartitionCount"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator FailedPartitionCount(int value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="FailedPartitionCount"/> to an <see cref="int"/>.
    /// </summary>
    /// <param name="count">The count to convert.</param>
    public static implicit operator int(FailedPartitionCount count) => count.Value;

    /// <summary>
    /// Adds an <see cref="int"/> to the <see cref="FailedPartitionCount"/>.
    /// </summary>
    /// <param name="left">The <see cref="FailedPartitionCount"/> to add to.</param>
    /// <param name="right">The value to add.</param>
    /// <returns>A new <see cref="FailedPartitionCount"/> with the updated value.</returns>
    public static FailedPartitionCount operator +(FailedPartitionCount left, int right) => new(left.Value + right);

    /// <summary>
    /// Subtracts an <see cref="int"/> from the <see cref="FailedPartitionCount"/>.
    /// </summary>
    /// <param name="left">The <see cref="FailedPartitionCount"/> to subtract from.</param>
    /// <param name="right">The value to subtract.</param>
    /// <returns>A new <see cref="FailedPartitionCount"/> with the updated value.</returns>
    public static FailedPartitionCount operator -(FailedPartitionCount left, int right) => new(Math.Max(0, left.Value - right));

    /// <summary>
    /// Increments the <see cref="FailedPartitionCount"/>.
    /// </summary>
    /// <param name="count">The count to increment.</param>
    /// <returns>A new <see cref="FailedPartitionCount"/> with the incremented value.</returns>
    public static FailedPartitionCount operator ++(FailedPartitionCount count) => count + 1;

    /// <summary>
    /// Decrements the <see cref="FailedPartitionCount"/>.
    /// </summary>
    /// <param name="count">The count to decrement.</param>
    /// <returns>A new <see cref="FailedPartitionCount"/> with the decremented value.</returns>
    public static FailedPartitionCount operator --(FailedPartitionCount count) => count - 1;
}

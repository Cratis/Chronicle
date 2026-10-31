// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when the maximum number of concurrently handled partitions is less than one.
/// </summary>
/// <param name="value">The invalid value.</param>
public class InvalidMaxConcurrentPartitions(int value)
    : Exception($"MaxConcurrentPartitions must be at least 1, but was {value}. Use 1 to handle partitions one at a time.")
{
    /// <summary>
    /// Throws when the value is not at least one.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <exception cref="InvalidMaxConcurrentPartitions">Thrown when the value is less than one.</exception>
    public static void ThrowIfInvalid(int value)
    {
        if (value < 1)
        {
            throw new InvalidMaxConcurrentPartitions(value);
        }
    }
}

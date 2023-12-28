// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Workers;

/// <summary>
/// Exception that gets thrown when the maximum concurrency level is invalid.
/// </summary>
public class InvalidMaximumConcurrencyLevel : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidMaximumConcurrencyLevel"/> class.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The degree of parallelism configured.</param>
    public InvalidMaximumConcurrencyLevel(int maxDegreeOfParallelism)
        : base($"Invalid maximum concurrency level: {maxDegreeOfParallelism}. Must be at least 1.")
    {
    }
}

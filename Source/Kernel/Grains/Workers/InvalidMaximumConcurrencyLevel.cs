// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Grains.Workers;

/// <summary>
/// Exception that gets thrown when the maximum concurrency level is invalid.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InvalidMaximumConcurrencyLevel"/> class.
/// </remarks>
/// <param name="maxDegreeOfParallelism">The degree of parallelism configured.</param>
public class InvalidMaximumConcurrencyLevel(int maxDegreeOfParallelism)
    : Exception($"Invalid maximum concurrency level: {maxDegreeOfParallelism}. Must be at least 1.");

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// The exception that is thrown when only a confirmed prefix of handled-count increments was applied.
/// </summary>
/// <remarks>
/// The first <see cref="AppliedCount"/> entries of the argument, in its enumeration order, are confirmed applied;
/// the rest are confirmed not applied.
/// </remarks>
/// <param name="appliedCount">The number of input entries confirmed applied.</param>
/// <param name="innerException">The underlying storage failure.</param>
public class ObserverHandledCountsPartiallyIncremented(int appliedCount, Exception innerException)
    : Exception($"Only the first {appliedCount} handled-count entries were applied: {innerException.Message}", innerException)
{
    /// <summary>
    /// Gets the number of input entries confirmed applied.
    /// </summary>
    public int AppliedCount { get; } = appliedCount;
}

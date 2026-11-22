// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Reports a confirmed applied prefix of a bulk handled-count increment. Only the remaining suffix should be retried.
/// </summary>
/// <param name="appliedCount">The number of applied entries in the original input, including empty entries before the failed entry.</param>
/// <param name="innerException">The storage failure that stopped the increment.</param>
public class ObserverHandledCountsPartiallyIncremented(int appliedCount, Exception innerException)
    : Exception("Observer handled counts were partially incremented.", innerException)
{
    /// <summary>
    /// Gets the number of original input entries confirmed applied before the failed entry.
    /// </summary>
    public int AppliedCount { get; } = appliedCount;
}

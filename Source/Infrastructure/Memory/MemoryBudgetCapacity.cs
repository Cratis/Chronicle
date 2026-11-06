// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Memory;

/// <summary>
/// Provides ways of deciding how large a <see cref="MemoryBudget"/> is.
/// </summary>
public static class MemoryBudgetCapacity
{
    static readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets a capacity that is a share of the memory available to the process, and shrinks while the process is
    /// under memory pressure.
    /// </summary>
    /// <param name="percentOfAvailableMemory">The share of the available memory - the heap hard limit or container limit when there is one, otherwise the memory of the machine - to budget, from 1 to 100.</param>
    /// <param name="percentWhenUnderPressure">The share of the budget that remains while the garbage collector reports a high memory load, from 1 to 100.</param>
    /// <param name="timeProvider">Optional <see cref="TimeProvider"/>.</param>
    /// <returns>A function that gives the capacity in bytes.</returns>
    /// <remarks>
    /// The garbage collector is asked at most once a second, since reserving can happen very often.
    /// </remarks>
    public static Func<long> OfAvailableMemory(int percentOfAvailableMemory, int percentWhenUnderPressure = 50, TimeProvider? timeProvider = null)
    {
        var clock = timeProvider ?? TimeProvider.System;
        var share = Math.Clamp(percentOfAvailableMemory, 1, 100) / 100d;
        var pressureShare = Math.Clamp(percentWhenUnderPressure, 1, 100) / 100d;
        var gate = new Lock();
        var lastRefresh = long.MinValue;
        var capacity = 0L;

        return () =>
        {
            lock (gate)
            {
                var now = clock.GetTimestamp();
                if (lastRefresh != long.MinValue && clock.GetElapsedTime(lastRefresh, now) < _refreshInterval)
                {
                    return capacity;
                }

                var info = GC.GetGCMemoryInfo();
                var available = info.TotalAvailableMemoryBytes;
                var underPressure = info.HighMemoryLoadThresholdBytes > 0 && info.MemoryLoadBytes >= info.HighMemoryLoadThresholdBytes;
                capacity = (long)(available * share * (underPressure ? pressureShare : 1d));
                lastRefresh = now;
                return capacity;
            }
        };
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Defines the absolute workload deadline independently of fault and settle operations.
/// </summary>
internal static class RunTiming
{
    /// <summary>
    /// Calculates remaining workload time, clamped at the duration-plus-settle deadline.
    /// </summary>
    /// <param name="duration">The configured fault window.</param>
    /// <param name="settle">The configured drain allowance.</param>
    /// <param name="elapsed">The time since both workers became ready.</param>
    /// <returns>The remaining workload budget, or zero when expired.</returns>
    internal static TimeSpan Remaining(TimeSpan duration, TimeSpan settle, TimeSpan elapsed)
    {
        var remaining = duration + settle - elapsed;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}

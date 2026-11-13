// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Generates a seeded, serialized fault schedule.
/// </summary>
internal static class FaultSchedule
{
    /// <summary>
    /// Plans bounded fault and recovery pairs.
    /// </summary>
    /// <param name="options">The seed, window, and selected faults.</param>
    /// <returns>The schedule in execution order.</returns>
    public static IReadOnlyList<ScheduledFault> Create(RunOptions options)
    {
        if (options.Faults.Count == 0) return [];
        var random = new Random(options.Seed);
        var result = new List<ScheduledFault>();
        var at = TimeSpan.FromSeconds(random.Next(3, 7));
        while (at + TimeSpan.FromSeconds(25) < options.Duration)
        {
            var kind = options.Faults[random.Next(options.Faults.Count)];
            result.Add(new(at, kind));

            // Global serialization is stronger than per-target exclusion. Long stops cross the 5s lease TTL.
            var hold = TimeSpan.FromSeconds(random.Next(16, 22));
            result.Add(new(at + hold, kind, Resume: true));
            at += hold + TimeSpan.FromSeconds(random.Next(4, 9));
        }
        return result;
    }
}

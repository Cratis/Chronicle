// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts.Servers;

namespace Cratis.Chronicle.Servers;

/// <summary>
/// Represents an implementation of <see cref="IServerInstance"/>.
/// </summary>
[ServerInstancePlacement]
public class ServerInstance : Grain, IServerInstance
{
    /// <summary>
    /// The window CPU usage is sampled over. Two <see cref="Process.TotalProcessorTime"/> readings this far
    /// apart are cheap enough to take on every call - no background sampling thread is kept running between
    /// requests - while still reflecting genuinely current usage rather than an average since process start.
    /// </summary>
    static readonly TimeSpan _cpuSampleWindow = TimeSpan.FromMilliseconds(200);

    /// <inheritdoc/>
    public async Task<ServerInstanceMetrics> GetMetrics()
    {
        var processorCount = Math.Max(Environment.ProcessorCount, 1);

        using var process = Process.GetCurrentProcess();
        var cpuTimeAtStart = process.TotalProcessorTime;
        var timestampAtStart = Stopwatch.GetTimestamp();

        await Task.Delay(_cpuSampleWindow);

        process.Refresh();
        var cpuTimeUsed = process.TotalProcessorTime - cpuTimeAtStart;
        var wallClockElapsed = Stopwatch.GetElapsedTime(timestampAtStart);
        var cpuTimeAvailable = wallClockElapsed * processorCount;

        var cpuUsagePercentage = cpuTimeAvailable > TimeSpan.Zero
            ? Math.Clamp(cpuTimeUsed / cpuTimeAvailable * 100, 0, 100)
            : 0;

        return new ServerInstanceMetrics
        {
            CpuUsagePercentage = cpuUsagePercentage,
            WorkingSetBytes = process.WorkingSet64
        };
    }
}

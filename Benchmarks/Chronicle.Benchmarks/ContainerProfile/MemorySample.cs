// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>A timestamped sample with process memory distinct from cgroup memory.</summary>
/// <param name="Timestamp">The sample time.</param>
/// <param name="Phase">The measurement phase.</param>
/// <param name="RssKb">Kernel PID 1 RSS, if available.</param>
/// <param name="HighWaterKb">Kernel PID 1 high-water RSS, if available.</param>
/// <param name="CgroupMemory">Docker cgroup memory usage, if available.</param>
/// <param name="CpuPercent">Docker CPU usage, if available.</param>
internal sealed record MemorySample(DateTimeOffset Timestamp, string Phase, long? RssKb, long? HighWaterKb, string? CgroupMemory, string? CpuPercent)
{
    /// <summary>Collects independent process and cgroup observations.</summary>
    /// <param name="context">The run context.</param>
    /// <param name="phase">The phase name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The sample, using null for unavailable fields.</returns>
    public static async Task<MemorySample> Capture(RunContext context, string phase, CancellationToken cancellationToken)
    {
        var status = await DockerCli.Run(MetricCommands.Status(context), cancellationToken);
        var stats = await DockerCli.Run(MetricCommands.Stats(context), cancellationToken);
        string? memory = null;
        string? cpu = null;
        if (stats.ExitCode == 0 && stats.StandardOutput.TrimStart().StartsWith('{'))
        {
            using var json = JsonDocument.Parse(stats.StandardOutput);
            memory = json.RootElement.GetProperty("MemUsage").GetString();
            cpu = json.RootElement.GetProperty("CPUPerc").GetString();
        }
        return new MemorySample(DateTimeOffset.UtcNow, phase, ReadKb(status.StandardOutput, "VmRSS:"), ReadKb(status.StandardOutput, "VmHWM:"), memory, cpu);
    }

    static long? ReadKb(string output, string key)
    {
        var field = output.Split('\n').FirstOrDefault(line => line.StartsWith(key, StringComparison.Ordinal));
        var value = field?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1);
        return long.TryParse(value, CultureInfo.InvariantCulture, out var number) ? number : null;
    }
}

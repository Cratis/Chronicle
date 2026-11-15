// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>A timestamped sample with process memory distinct from cgroup memory.</summary>
internal sealed record MemorySample
{
    /// <summary>Gets the sample time.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Gets the measurement phase.</summary>
    public required string Phase { get; init; }

    /// <summary>Gets kernel PID 1 RSS, if available.</summary>
    public long? RssKb { get; init; }

    /// <summary>Gets kernel PID 1 high-water RSS, if available.</summary>
    public long? HighWaterKb { get; init; }

    /// <summary>Gets Docker cgroup memory usage, if available.</summary>
    public string? CgroupMemory { get; init; }

    /// <summary>Gets Docker CPU usage, if available.</summary>
    public string? CpuPercent { get; init; }

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
        return new MemorySample
        {
            Timestamp = DateTimeOffset.UtcNow,
            Phase = phase,
            RssKb = ReadKb(status.StandardOutput, "VmRSS:"),
            HighWaterKb = ReadKb(status.StandardOutput, "VmHWM:"),
            CgroupMemory = memory,
            CpuPercent = cpu
        };
    }

    static long? ReadKb(string output, string key)
    {
        var field = output.Split('\n').FirstOrDefault(line => line.StartsWith(key, StringComparison.Ordinal));
        var value = field?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1);
        return long.TryParse(value, CultureInfo.InvariantCulture, out var number) ? number : null;
    }
}

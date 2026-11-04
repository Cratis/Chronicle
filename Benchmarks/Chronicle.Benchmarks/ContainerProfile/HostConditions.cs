// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Non-sensitive conditions captured before each repetition.</summary>
internal sealed record HostConditions
{
    /// <summary>Gets the capture timestamp.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Gets the host OS.</summary>
    public required string OS { get; init; }

    /// <summary>Gets the host architecture.</summary>
    public required string Architecture { get; init; }

    /// <summary>Gets the client CPU count.</summary>
    public required int LogicalCpus { get; init; }

    /// <summary>Gets the one-minute load.</summary>
    public required double Load1 { get; init; }

    /// <summary>Gets the five-minute load.</summary>
    public required double Load5 { get; init; }

    /// <summary>Gets the fifteen-minute load.</summary>
    public required double Load15 { get; init; }

    /// <summary>Gets projected Docker VM conditions.</summary>
    public required string Docker { get; init; }

    /// <summary>Gets the kernel image ID and bytes.</summary>
    public required string KernelImage { get; init; }

    /// <summary>Gets the MongoDB image ID and bytes.</summary>
    public required string MongoImage { get; init; }

    /// <summary>Gets whether the final load exceeds policy.</summary>
    public required bool NoisyHost { get; init; }

    /// <summary>Waits up to five minutes for acceptable host load, then records skip conditions.</summary>
    /// <param name="context">The run context.</param>
    /// <param name="cell">The cell.</param>
    /// <param name="options">The load policy.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The captured conditions.</returns>
    public static async Task<HostConditions> Capture(RunContext context, MatrixCell cell, ProfileOptions options, CancellationToken cancellationToken)
    {
        var loads = await ReadLoad(cancellationToken);
        var watch = Stopwatch.StartNew();
        while (ShouldWaitForQuietHost(options, loads[0], watch.Elapsed))
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            loads = await ReadLoad(cancellationToken);
        }
        return new HostConditions
        {
            Timestamp = DateTimeOffset.UtcNow,
            OS = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.OSArchitecture.ToString(),
            LogicalCpus = Environment.ProcessorCount,
            Load1 = loads[0],
            Load5 = loads[1],
            Load15 = loads[2],
            Docker = await DockerCli.Require(MetricCommands.Info(), cancellationToken),
            KernelImage = await DockerCli.Require(MetricCommands.Image(cell.Image.Reference(context.Sha)), cancellationToken),
            MongoImage = await DockerCli.Require(MetricCommands.Image(context.MongoImage), cancellationToken),
            NoisyHost = loads[0] > options.MaxHostLoad
        };
    }

    static bool ShouldWaitForQuietHost(ProfileOptions options, double load, TimeSpan elapsed)
    {
        if (options.AllowNoisyHost)
        {
            return false;
        }
        if (elapsed >= TimeSpan.FromMinutes(5))
        {
            return false;
        }
        return load > options.MaxHostLoad;
    }

    static async Task<double[]> ReadLoad(CancellationToken cancellationToken)
    {
        string output;
        if (OperatingSystem.IsLinux())
        {
            output = await File.ReadAllTextAsync("/proc/loadavg", cancellationToken);
        }
        else if (OperatingSystem.IsMacOS())
        {
            output = (await ProcessRunner.Run("sysctl", ["-n", "vm.loadavg"], TimeSpan.FromSeconds(10), cancellationToken)).RequireSuccess("read host load").StandardOutput;
        }
        else
        {
            throw new HarnessFailure("Load-average gating supports Linux and macOS only.");
        }
        return output.Replace("{", string.Empty, StringComparison.Ordinal).Replace("}", string.Empty, StringComparison.Ordinal)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Take(3).Select(value => double.Parse(value, CultureInfo.InvariantCulture)).ToArray();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Non-sensitive conditions captured before each repetition.</summary>
/// <param name="Timestamp">The timestamp.</param>
/// <param name="OS">The host OS.</param>
/// <param name="Architecture">The host architecture.</param>
/// <param name="LogicalCpus">The client CPU count.</param>
/// <param name="Load1">The one-minute load.</param>
/// <param name="Load5">The five-minute load.</param>
/// <param name="Load15">The fifteen-minute load.</param>
/// <param name="Docker">Projected Docker VM conditions.</param>
/// <param name="KernelImage">The kernel image ID and bytes.</param>
/// <param name="MongoImage">The MongoDB image ID and bytes.</param>
/// <param name="NoisyHost">Whether the final load exceeds policy.</param>
internal sealed record HostConditions(DateTimeOffset Timestamp, string OS, string Architecture, int LogicalCpus, double Load1, double Load5, double Load15, string Docker, string KernelImage, string MongoImage, bool NoisyHost)
{
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
        while (!options.AllowNoisyHost && loads[0] > options.MaxHostLoad && watch.Elapsed < TimeSpan.FromMinutes(5))
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            loads = await ReadLoad(cancellationToken);
        }
        return new HostConditions(
            DateTimeOffset.UtcNow,
            RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture.ToString(),
            Environment.ProcessorCount,
            loads[0],
            loads[1],
            loads[2],
            await DockerCli.Require(MetricCommands.Info(), cancellationToken),
            await DockerCli.Require(MetricCommands.Image(cell.Image.Reference(context.Sha)), cancellationToken),
            await DockerCli.Require(MetricCommands.Image(context.MongoImage), cancellationToken),
            loads[0] > options.MaxHostLoad);
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

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>The narrow container-state fields needed to distinguish OOM from workload failure.</summary>
/// <param name="ExitCode">The container exit code, if available.</param>
/// <param name="OomKilled">The Docker OOM observation, if available.</param>
/// <param name="Running">Whether the container is still running.</param>
internal sealed record KernelState(int? ExitCode, bool? OomKilled, bool? Running)
{
    /// <summary>Reads state without exposing container environment or diagnostics.</summary>
    /// <param name="context">The run context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The projected state.</returns>
    public static async Task<KernelState> Read(RunContext context, CancellationToken cancellationToken)
    {
        var result = await DockerCli.Run(MetricCommands.State(context), cancellationToken);
        if (result.ExitCode != 0)
        {
            return new KernelState(null, null, null);
        }
        using var json = JsonDocument.Parse(result.StandardOutput);
        var root = json.RootElement;
        return new KernelState(root.GetProperty(nameof(ExitCode)).GetInt32(), root.GetProperty("OOMKilled").GetBoolean(), root.GetProperty(nameof(Running)).GetBoolean());
    }
}

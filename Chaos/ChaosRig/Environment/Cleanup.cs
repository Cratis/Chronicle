// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Removes only Docker resources labeled as owned by the rig.
/// </summary>
internal static class Cleanup
{
    /// <summary>
    /// Removes a selected run or all labeled runs.
    /// </summary>
    /// <param name="runId">The run identity, or null for all runs.</param>
    /// <param name="cancellation">Cancellation for Docker commands.</param>
    /// <returns>The cleanup task.</returns>
    public static async Task Run(string? runId, CancellationToken cancellation)
    {
        var label = runId is null ? "chronicle-chaos" : $"chronicle-chaos={runId}";
        var containers = await NativeCommand.Run("docker", ["ps", "-aq", "--filter", $"label={label}"], cancellation);
        foreach (var id in Ids(containers)) await RemoveContainer(id, cancellation);
        var networks = await NativeCommand.Run("docker", ["network", "ls", "-q", "--filter", $"label={label}"], cancellation);
        foreach (var id in Ids(networks)) await NativeCommand.Run("docker", ["network", "rm", id], cancellation);
    }

    static async Task RemoveContainer(string id, CancellationToken cancellation)
    {
        var paused = await NativeCommand.Run("docker", ["inspect", "--format", "{{.State.Paused}}", id], cancellation);
        if (paused.Trim() == "true") await ContainerSignals.Unpause(id, cancellation);
        await NativeCommand.Run("docker", ["rm", "-f", "-v", id], cancellation);
    }

    static string[] Ids(string output) => output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

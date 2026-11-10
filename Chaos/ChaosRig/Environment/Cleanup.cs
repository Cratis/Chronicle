// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

internal static class Cleanup
{
    public static async Task Run(string? runId, CancellationToken cancellation)
    {
        var label = runId is null ? "chronicle-chaos" : $"chronicle-chaos={runId}";
        var containers = await NativeCommand.Run("docker", ["ps", "-aq", "--filter", $"label={label}"], cancellation);
        foreach (var id in Ids(containers)) await NativeCommand.Run("docker", ["rm", "-f", "-v", id], cancellation);
        var networks = await NativeCommand.Run("docker", ["network", "ls", "-q", "--filter", $"label={label}"], cancellation);
        foreach (var id in Ids(networks)) await NativeCommand.Run("docker", ["network", "rm", id], cancellation);
    }

    static IEnumerable<string> Ids(string output) => output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

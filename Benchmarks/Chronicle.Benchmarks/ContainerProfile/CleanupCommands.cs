// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Builds exact-target or label-scoped cleanup, never daemon-wide cleanup.</summary>
internal static class CleanupCommands
{
    /// <summary>Removes known containers and their anonymous volumes.</summary>
    /// <param name="containers">The exact container identities.</param>
    /// <returns>The bounded command.</returns>
    public static DockerCommand Remove(params string[] containers) => new("remove containers", ["rm", "-f", "-v", .. containers], TimeSpan.FromSeconds(60));

    /// <summary>Removes a known network.</summary>
    /// <param name="network">The exact network identity.</param>
    /// <returns>The bounded command.</returns>
    public static DockerCommand RemoveNetwork(string network) => new("remove network", ["network", "rm", network], TimeSpan.FromSeconds(60));

    /// <summary>Finds only harness-labelled containers owned by one session.</summary>
    /// <param name="session">The session identity.</param>
    /// <returns>The bounded command.</returns>
    public static DockerCommand Containers(string session) => new("list labelled containers", ["ps", "-aq", "--filter", "label=cratis.container-profile=1", "--filter", $"label=cratis.container-profile.session={session}"], TimeSpan.FromSeconds(30));

    /// <summary>Finds only harness-labelled networks owned by one session.</summary>
    /// <param name="session">The session identity.</param>
    /// <returns>The bounded command.</returns>
    public static DockerCommand Networks(string session) => new("list labelled networks", ["network", "ls", "-q", "--filter", "label=cratis.container-profile=1", "--filter", $"label=cratis.container-profile.session={session}"], TimeSpan.FromSeconds(30));

    /// <summary>Removes exact IDs selected by both harness ownership and session labels.</summary>
    /// <param name="session">The required session identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cleanup task.</returns>
    public static async Task Run(string session, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(session);
        var containers = await DockerCli.Require(Containers(session), cancellationToken);
        foreach (var id in Split(containers))
        {
            await DockerCli.Require(Remove(id), cancellationToken);
        }
        var networks = await DockerCli.Require(Networks(session), cancellationToken);
        foreach (var id in Split(networks))
        {
            await DockerCli.Require(RemoveNetwork(id), cancellationToken);
        }
    }

    static string[] Split(string output) => output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

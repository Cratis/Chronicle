// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Executes Docker argument vectors without a shell.</summary>
internal static class DockerCli
{
    /// <summary>Runs a bounded Docker command.</summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The raw process outcome.</returns>
    public static Task<ProcessResult> Run(DockerCommand command, CancellationToken cancellationToken) =>
        ProcessRunner.Run("docker", command.Arguments, command.Deadline, cancellationToken);

    /// <summary>Runs a Docker command that must succeed.</summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Trimmed standard output.</returns>
    public static async Task<string> Require(DockerCommand command, CancellationToken cancellationToken) =>
        (await Run(command, cancellationToken)).RequireSuccess(command.Purpose).StandardOutput.Trim();
}

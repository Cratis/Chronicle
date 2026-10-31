// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Reads fresh Docker exit state and bounded logs before a failed container is removed.
/// </summary>
public static class DockerContainerEvidence
{
    /// <summary>
    /// Inspects Docker rather than relying on Testcontainers' cached state.
    /// </summary>
    /// <param name="container">The container to inspect.</param>
    /// <param name="cancellationToken">The evidence collection deadline.</param>
    /// <returns>The exit state, or null if the container has not exited.</returns>
    public static async Task<ContainerExit?> Inspect(IContainer container, CancellationToken cancellationToken)
    {
        using var client = TestcontainersSettings.OS.DockerEndpointAuthConfig.GetDockerClientBuilder(Guid.NewGuid()).Build();
        var response = await client.Containers.InspectContainerAsync(container.Id, cancellationToken);
        return response.State is { Running: false } state &&
            (string.Equals(state.Status, "exited", StringComparison.Ordinal) || string.Equals(state.Status, "dead", StringComparison.Ordinal))
            ? new(state.ExitCode, state.OOMKilled, state.Error)
            : null;
    }

    /// <summary>
    /// Reads at most 16 KiB of the requested log tail.
    /// </summary>
    /// <param name="container">The container whose logs to read.</param>
    /// <param name="lines">The maximum number of log lines requested.</param>
    /// <param name="cancellationToken">The evidence collection deadline.</param>
    /// <returns>The captured UTF-8 log tail.</returns>
    public static async Task<string> ReadTail(IContainer container, int lines, CancellationToken cancellationToken)
    {
        using var client = TestcontainersSettings.OS.DockerEndpointAuthConfig.GetDockerClientBuilder(Guid.NewGuid()).Build();
        using var stream = await client.Containers.GetContainerLogsAsync(
            container.Id,
            new ContainerLogsParameters { ShowStdout = true, ShowStderr = true, Tail = lines.ToString(CultureInfo.InvariantCulture), Follow = false },
            cancellationToken);
        var bytes = new byte[16384];
        var count = 0;
        while (count < bytes.Length)
        {
            var result = await stream.ReadOutputAsync(bytes, count, bytes.Length - count, cancellationToken);
            if (result.EOF)
            {
                break;
            }
            count += result.Count;
        }
        return Encoding.UTF8.GetString(bytes, 0, count);
    }
}

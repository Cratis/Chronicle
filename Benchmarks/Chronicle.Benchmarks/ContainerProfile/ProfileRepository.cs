// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Resolves repository paths and source identity without changing Git state.</summary>
internal static class ProfileRepository
{
    /// <summary>Finds the containing Chronicle checkout.</summary>
    /// <returns>The repository root.</returns>
    /// <exception cref="HarnessFailure">No Chronicle checkout contains the working directory.</exception>
    public static string Root()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Chronicle.slnx")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new HarnessFailure("Run from the Chronicle repository.");
    }

    /// <summary>Reads the abbreviated source commit.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The source SHA.</returns>
    public static async Task<string> Sha(CancellationToken cancellationToken) =>
        (await ProcessRunner.Run("git", ["-C", Root(), "rev-parse", "--short=10", "HEAD"], TimeSpan.FromSeconds(30), cancellationToken))
            .RequireSuccess("read source commit").StandardOutput.Trim();
}

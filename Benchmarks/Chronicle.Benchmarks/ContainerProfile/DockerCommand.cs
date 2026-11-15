// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>A bounded Docker invocation shared by planning and execution.</summary>
/// <param name="Purpose">A non-sensitive description.</param>
/// <param name="Arguments">The argument vector.</param>
/// <param name="Deadline">The execution deadline.</param>
internal sealed record DockerCommand(string Purpose, IReadOnlyList<string> Arguments, TimeSpan Deadline)
{
    /// <summary>Gets a shell-readable rendering; only render placeholder credentials.</summary>
    public string Display => $"docker {string.Join(' ', Arguments.Select(Quote))} (deadline {Deadline.TotalSeconds}s)";

    static string Quote(string argument) => $"'{argument.Replace("'", "'\\''", StringComparison.Ordinal)}'";
}

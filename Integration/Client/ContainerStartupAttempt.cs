// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Captures evidence from one startup crash before the container is disposed.
/// </summary>
/// <param name="Attempt">The one-based attempt number.</param>
/// <param name="MaxAttempts">The maximum number of starts.</param>
/// <param name="ContainerId">The failed container identifier.</param>
/// <param name="Exit">The fresh exit state, if available.</param>
/// <param name="LogTail">The bounded log tail and any evidence collection errors.</param>
/// <param name="Failure">The original startup failure.</param>
public sealed record ContainerStartupAttempt(int Attempt, int MaxAttempts, string ContainerId, ContainerExit? Exit, string LogTail, Exception Failure)
{
    /// <summary>
    /// Describes the attempt and its captured evidence.
    /// </summary>
    /// <returns>A summary followed by the log tail.</returns>
    public string Describe() =>
        $"attempt {Attempt}/{MaxAttempts}, container {ContainerId}: " +
        (Exit is null ? "exit state unavailable" : $"exit code {Exit.ExitCode}, OOM killed {Exit.OomKilled}, error {Exit.Error}") +
        $"; {Failure.GetType().Name}: {Failure.Message}{Environment.NewLine}{LogTail}";
}

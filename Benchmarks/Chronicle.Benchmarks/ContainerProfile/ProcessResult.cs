// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>The outcome of a bounded child process.</summary>
/// <param name="ExitCode">The producer exit code, or 124 for a deadline.</param>
/// <param name="StandardOutput">Captured output, potentially sensitive.</param>
/// <param name="StandardError">Captured diagnostics, potentially sensitive.</param>
/// <param name="ElapsedMs">Wall time in milliseconds.</param>
internal sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError, double ElapsedMs)
{
    /// <summary>Rejects an unsuccessful invocation without exposing its output.</summary>
    /// <param name="purpose">A non-sensitive description.</param>
    /// <returns>This successful result.</returns>
    /// <exception cref="HarnessFailure">The child process did not succeed.</exception>
    public ProcessResult RequireSuccess(string purpose)
    {
        if (ExitCode != 0)
        {
            // Child output can contain credentials. Keep it out of logs and persisted results.
            throw new HarnessFailure($"{purpose} failed (exit {ExitCode}); child output withheld to protect credentials.");
        }
        return this;
    }
}

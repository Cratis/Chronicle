// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Records why the rig could not produce a complete verdict.
/// </summary>
/// <param name="Seed">The fault schedule seed.</param>
/// <param name="RunId">The isolated run identifier.</param>
/// <param name="Status">The timeout, canceled, or could-not-run classification.</param>
/// <param name="Reason">The diagnostic exception details.</param>
internal sealed record RunFailure(int Seed, string RunId, string Status, string Reason)
{
    /// <summary>
    /// Gets the README's could-not-run exit code, never a dirty durability verdict.
    /// </summary>
    public int ExitCode => 1;

    /// <summary>
    /// Classifies an incomplete run without interpreting it as a product defect.
    /// </summary>
    /// <param name="seed">The schedule seed.</param>
    /// <param name="runId">The run identifier.</param>
    /// <param name="exception">The failure that prevented completion.</param>
    /// <param name="userCanceled">Whether the caller requested cancellation.</param>
    /// <returns>The explicit incomplete-run result.</returns>
    internal static RunFailure From(int seed, string runId, Exception exception, bool userCanceled)
    {
        var status = exception switch
        {
            OperationCanceledException when userCanceled => "canceled",
            OperationCanceledException or TimeoutException => "timeout",
            _ => "could-not-run"
        };
        return new(seed, runId, status, exception.ToString());
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Decides how a run of the kernel host ended and which process exit code that gives.
/// </summary>
/// <remarks>
/// An orderly stop exits with the exit code the kernel requested, which is zero unless a component asked for a
/// non-zero one (a lost single-writer lease does). A stop that times out or fails exits with
/// <see cref="FailedExitCode"/>, so platforms that count non-zero exits as crashes still see it. Returning the
/// decided code from the entry point matters: an entry point that returns a value replaces
/// <see cref="Environment.ExitCode"/>, so a fixed zero would hide every requested non-zero code (#5813).
/// </remarks>
internal static class KernelExit
{
    /// <summary>
    /// The exit code for a run whose stop timed out or failed: EX_SOFTWARE, the same code as an unhandled exception.
    /// </summary>
    internal const int FailedExitCode = BackgroundFaults.UnhandledExceptionExitCode;

    /// <summary>
    /// Classifies how a run of the host ended.
    /// </summary>
    /// <param name="failure">The exception the run ended with, or <see langword="null"/> when it returned.</param>
    /// <param name="started">Whether the host reported that it had started.</param>
    /// <param name="stopped">Whether the host reported that it had stopped.</param>
    /// <returns>The <see cref="KernelRunOutcome"/>.</returns>
    internal static KernelRunOutcome Classify(Exception? failure, bool started, bool stopped) => failure switch
    {
        null => KernelRunOutcome.Completed,
        _ when stopped => KernelRunOutcome.DisposeFailed,
        OperationCanceledException when !started => KernelRunOutcome.StoppedDuringStartup,
        _ when !started => KernelRunOutcome.StartFailed,
        OperationCanceledException => KernelRunOutcome.StopTimedOut,
        _ => KernelRunOutcome.StopFailed
    };

    /// <summary>
    /// Gets the process exit code for an outcome.
    /// </summary>
    /// <param name="outcome">How the run ended.</param>
    /// <param name="requestedExitCode">The exit code the kernel requested through <see cref="Environment.ExitCode"/>.</param>
    /// <returns>The process exit code.</returns>
    internal static int CodeFor(KernelRunOutcome outcome, int requestedExitCode) => outcome switch
    {
        KernelRunOutcome.Completed or KernelRunOutcome.StoppedDuringStartup => requestedExitCode,
        _ => requestedExitCode == 0 ? FailedExitCode : requestedExitCode
    };
}

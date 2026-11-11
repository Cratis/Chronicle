// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Decides how a run of the kernel host ended and which process exit code that gives.
/// </summary>
/// <remarks>
/// An orderly stop exits with the exit code the kernel requested, which is zero unless a component asked for a
/// non-zero one (a lost single-writer lease does). A start, stop or release that fails, or a stop that times out,
/// exits with <see cref="FailedExitCode"/>, so platforms that count non-zero exits as crashes still see it - unless
/// the kernel requested a non-zero code, which takes precedence. Returning the decided code from the entry point
/// matters: an entry point that returns a value replaces <see cref="Environment.ExitCode"/>, so a fixed zero would
/// hide every requested non-zero code.
/// </remarks>
internal static class KernelExit
{
    /// <summary>
    /// The exit code for a run that failed or whose stop timed out: EX_SOFTWARE, the same code as an unhandled exception.
    /// </summary>
    internal const int FailedExitCode = BackgroundFaults.UnhandledExceptionExitCode;

    /// <summary>
    /// Checks whether a failure while starting is the host giving up on starting because a stop was requested.
    /// </summary>
    /// <param name="failure">The exception starting ended with.</param>
    /// <param name="stopRequested">Whether a stop had been requested when starting ended.</param>
    /// <returns><see langword="true"/> for a requested stop, <see langword="false"/> for a failure to start.</returns>
    internal static bool IsStopDuringStartup(Exception? failure, bool stopRequested) =>
        stopRequested && failure is OperationCanceledException;

    /// <summary>
    /// Classifies how a run of the host ended.
    /// </summary>
    /// <param name="phase">The <see cref="KernelRunPhase"/> the run ended in.</param>
    /// <param name="failure">The exception the run ended with, or <see langword="null"/> when nothing failed.</param>
    /// <param name="stopRequested">Whether a stop had been requested when the run ended.</param>
    /// <param name="stopTimedOut">Whether the stop took the whole host shutdown timeout.</param>
    /// <returns>The <see cref="KernelRunOutcome"/>.</returns>
    internal static KernelRunOutcome Classify(KernelRunPhase phase, Exception? failure, bool stopRequested, bool stopTimedOut) => phase switch
    {
        KernelRunPhase.Starting when IsStopDuringStartup(failure, stopRequested) => KernelRunOutcome.StoppedDuringStartup,
        KernelRunPhase.Starting => KernelRunOutcome.StartFailed,
        _ when stopTimedOut => KernelRunOutcome.StopTimedOut,
        KernelRunPhase.Stopping => KernelRunOutcome.StopFailed,
        KernelRunPhase.Disposing => KernelRunOutcome.DisposeFailed,
        _ => KernelRunOutcome.Completed
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

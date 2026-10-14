// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Classifies exceptions raised by code that only observes, such as metrics and diagnostic logging.
/// </summary>
/// <remarks>
/// Observation-only code must never change what the kernel does, so its own failures are contained where they happen.
/// Failures that mean the process itself is broken are never contained: they say nothing about the observation and
/// must reach the runtime.
/// </remarks>
public static class ObservationOnlyFailures
{
    /// <summary>
    /// Check whether an exception raised by observation-only code can be contained without hiding a broken process.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to check.</param>
    /// <returns>True if the exception can be contained; false if it must propagate.</returns>
    public static bool CanBeContained(Exception exception) =>
        exception is not (OutOfMemoryException or StackOverflowException or AccessViolationException or InsufficientExecutionStackException);
}

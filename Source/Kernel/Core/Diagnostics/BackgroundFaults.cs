// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Setup;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Helpers for work that runs without a caller to report a failure to: fire-and-forget tasks and unobserved task exceptions.
/// </summary>
/// <remarks>
/// A transient storage failure, such as a full MongoDB connection wait queue, is something the kernel already
/// classifies and retries on a grain call. On a background path nobody is there to retry, so it is logged once
/// with the work that owned it instead of taking the process down (#5687).
/// </remarks>
public static class BackgroundFaults
{
    /// <summary>
    /// The process exit code used when the kernel terminates on an unhandled exception.
    /// </summary>
    public const int UnhandledExceptionExitCode = 70;

    /// <summary>
    /// Determines whether every failure in an exception, including aggregated and inner exceptions, is transient.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to judge.</param>
    /// <returns><see langword="true"/> when the failure is one the kernel classifies as transient.</returns>
    public static bool IsTransient(Exception exception) => exception switch
    {
        AggregateException aggregate => aggregate.InnerExceptions.Count > 0 && aggregate.InnerExceptions.All(IsTransient),
        _ => SiblingSiloInstability.IsTransient(exception) || (exception.InnerException is { } inner && IsTransient(inner))
    };

    /// <summary>
    /// Describes which type and method an exception was thrown from, for naming the owner of an unobserved failure.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to describe.</param>
    /// <returns>The owner as Type.Method, or the exception type when no frame is available.</returns>
    public static string DescribeOwner(Exception exception)
    {
        var innermost = exception;
        while (true)
        {
            if (innermost is AggregateException { InnerExceptions.Count: > 0 } aggregate)
            {
                innermost = aggregate.InnerExceptions[0];
            }
            else if (innermost.InnerException is { } inner)
            {
                innermost = inner;
            }
            else
            {
                break;
            }
        }

        foreach (var frame in new StackTrace(innermost, false).GetFrames())
        {
            var method = frame.GetMethod();
            if (method?.DeclaringType is { } type && type.Namespace?.StartsWith("Cratis", StringComparison.Ordinal) == true)
            {
                return $"{type.FullName}.{method.Name}";
            }
        }

        return innermost.GetType().FullName ?? innermost.GetType().Name;
    }

    /// <summary>
    /// Runs background work on the thread pool and observes any failure instead of leaving it unobserved.
    /// </summary>
    /// <param name="work">The work to run.</param>
    /// <param name="logger">The <see cref="ILogger"/> that reports a failure.</param>
    /// <param name="owner">What owns the work, named in the log entry.</param>
    /// <returns>A <see cref="Task"/> that never faults, except by cancellation.</returns>
    public static Task Run(Func<Task> work, ILogger logger, string owner) =>
        Task.Run(async () =>
        {
            try
            {
                await work();
            }
            catch (OperationCanceledException)
            {
                // Cancellation is an orderly stop, not a failure.
            }
            catch (Exception ex)
            {
                if (IsTransient(ex))
                {
                    logger.BackgroundWorkTransientFailure(ex, owner);
                }
                else
                {
                    logger.BackgroundWorkFailed(ex, owner);
                }
            }
        });
}

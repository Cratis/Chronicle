// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

/// <summary>
/// Starts background work without inheriting its caller's execution context.
/// </summary>
/// <remarks>
/// Suppression drops all AsyncLocals, including activities, logging scopes and RequestContext. This is intended for
/// loops that outlive their caller, not work belonging to a request.
/// </remarks>
public static class DetachedExecution
{
    /// <summary>
    /// Runs background work on the thread pool without flowing the execution context.
    /// </summary>
    /// <param name="work">The work to run.</param>
    /// <param name="cancellationToken">The cancellation token for scheduling the work.</param>
    /// <returns>The background task.</returns>
    public static Task Run(Func<Task> work, CancellationToken cancellationToken = default)
    {
        if (ExecutionContext.IsFlowSuppressed())
        {
            return Task.Run(work, cancellationToken);
        }

        Task task;
        using (ExecutionContext.SuppressFlow())
        {
            task = Task.Run(work, cancellationToken);
        }

        return task;
    }

    /// <summary>
    /// Starts work using its own scheduler without flowing the execution context.
    /// </summary>
    /// <typeparam name="T">The scheduler's return type.</typeparam>
    /// <param name="start">The operation that schedules the work.</param>
    /// <returns>The scheduler's result.</returns>
    public static T Start<T>(Func<T> start)
    {
        if (ExecutionContext.IsFlowSuppressed())
        {
            return start();
        }

        using (ExecutionContext.SuppressFlow())
        {
            return start();
        }
    }
}

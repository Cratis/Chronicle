// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.ExceptionServices;

namespace Cratis.Chronicle.Benchmarks.Storage.for_AllocationAttribution;

/// <summary>Allocation-only diagnostics; incomplete asynchronous work is rejected, never blocked or undercounted.</summary>
internal static class AllocationProbe
{
    /// <summary>Runs setup, warmup, sampling and cleanup on one dedicated thread.</summary>
    /// <typeparam name="T">The diagnostic result.</typeparam>
    /// <param name="work">The synchronous diagnostic.</param>
    /// <returns>The diagnostic result.</returns>
    public static T OnThread<T>(Func<T> work)
    {
        T result = default!;
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = work();
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
        });
        thread.Start();
        thread.Join();
        failure?.Throw();
        return result;
    }

    /// <summary>Consumes an already-successful Task, refusing current-thread undercounts.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="task">The operation's Task.</param>
    /// <returns>The completed result.</returns>
    public static T Complete<T>(Task<T> task)
    {
        RequireCompleted(task);
        return task.GetAwaiter().GetResult();
    }

    /// <summary>Consumes an already-successful Task, refusing current-thread undercounts.</summary>
    /// <param name="task">The operation's Task.</param>
    public static void Complete(Task task)
    {
        RequireCompleted(task);
        task.GetAwaiter().GetResult();
    }

    /// <summary>Measures warmed current-thread allocations, not elapsed time.</summary>
    /// <param name="operation">The synchronous boundary, constructed before sampling.</param>
    /// <param name="iterations">The number of warmup and measured calls.</param>
    /// <returns>Mean allocated bytes per call.</returns>
    /// <exception cref="InvalidOperationException">The operation changes threads.</exception>
    public static double Bytes(Action operation, int iterations = 64)
    {
        for (var i = 0; i < iterations; i++) operation();
        var thread = Environment.CurrentManagedThreadId;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < iterations; i++)
        {
            operation();
            if (Environment.CurrentManagedThreadId != thread) throw new InvalidOperationException("Allocation diagnostic changed threads.");
        }
        return (GC.GetAllocatedBytesForCurrentThread() - before) / (double)iterations;
    }

    static void RequireCompleted(Task task)
    {
        if (!task.IsCompletedSuccessfully) throw new InvalidOperationException($"Allocation boundary did not complete synchronously and successfully ({task.Status}). No allocation result is valid.");
    }
}

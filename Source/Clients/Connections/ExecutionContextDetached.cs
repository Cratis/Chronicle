// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Starts connection-lifetime tasks without capturing the caller's execution context.
/// </summary>
internal static class ExecutionContextDetached
{
    /// <summary>
    /// Starts a task with execution-context flow suppressed, restoring flow before returning it.
    /// </summary>
    /// <param name="start">The task factory. Inline work must be scheduled by the factory to run in a clean context.</param>
    /// <returns>The started task, which can be awaited outside the suppression scope.</returns>
    internal static Task Start(Func<Task> start)
    {
        if (ExecutionContext.IsFlowSuppressed())
        {
            return start();
        }

        Task task;
        using (ExecutionContext.SuppressFlow())
        {
            task = start();
        }

        return task;
    }
}

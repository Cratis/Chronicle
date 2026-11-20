// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Extension methods for bounding waits by the shared kernel stop deadline held by <see cref="IShutdownDrainBudget"/>.
/// </summary>
internal static class ShutdownDrainBudgetExtensions
{
    /// <summary>
    /// Awaits a call, abandoning the wait at the shared stop deadline. The token only cancels at the deadline, which only exists
    /// once the stop has begun, so a call issued before the stop is bounded as well and attaching it costs nothing otherwise.
    /// </summary>
    /// <param name="budget">The shared kernel stop deadline; none means nothing is bounded.</param>
    /// <param name="call">The call to await.</param>
    /// <returns>True if the call completed; false if the deadline passed first and the call was abandoned.</returns>
    /// <remarks>
    /// An abandoned call keeps running; its outcome is observed so a late failure is not an unobserved task exception.
    /// </remarks>
    internal static async Task<bool> CompletesWithinStopBudget(this IShutdownDrainBudget? budget, Task call)
    {
        if (budget is null)
        {
            await call;
            return true;
        }

        return await call.CompletesBefore(budget.Token);
    }

    /// <summary>
    /// Awaits a call, abandoning the wait when a deadline token is cancelled.
    /// </summary>
    /// <param name="call">The call to await.</param>
    /// <param name="deadline">The token that is cancelled when the deadline passes.</param>
    /// <returns>True if the call completed; false if the deadline passed first and the call was abandoned.</returns>
    /// <remarks>
    /// A call that completed while the deadline was cancelling the wait keeps its real outcome: it is awaited, so a failure
    /// still surfaces and a success is not reported as abandoned. A call that is still running is abandoned and its outcome
    /// observed so a late failure is not an unobserved task exception.
    /// </remarks>
    internal static async Task<bool> CompletesBefore(this Task call, CancellationToken deadline)
    {
        try
        {
            await call.WaitAsync(deadline);
            return true;
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            if (call.IsCompleted)
            {
                await call;
                return true;
            }

            _ = call.ContinueWith(static task => _ = task.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return false;
        }
    }
}

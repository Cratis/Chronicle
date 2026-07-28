// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans;

/// <summary>
/// Extension methods for deferring work to a separate grain turn.
/// </summary>
internal static class GrainDeferralExtensions
{
    /// <summary>
    /// Schedule work to run exactly once in a separate grain turn.
    /// </summary>
    /// <param name="grain"><see cref="IGrainBase"/> to schedule the work on.</param>
    /// <param name="work">The work to run.</param>
    /// <remarks>
    /// The work runs after the current grain call has returned, so it is free to call back into this
    /// grain without deadlocking against the execution slot the current call still holds. It does not
    /// interleave, so it waits for that slot rather than running beside the call that scheduled it.
    /// <para>
    /// Orleans keeps a fired timer registered on the activation until it is disposed, and this timer
    /// is non-periodic, so it disposes itself as soon as it runs. Disposing before the work rather
    /// than after is deliberate: it releases the timer even when the work throws.
    /// </para>
    /// <para>
    /// The <see cref="CancellationToken"/> Orleans hands to the timer callback is deliberately not
    /// passed on, because disposing the timer is what cancels it. Threading it into the work would
    /// hand the work a token that is already cancelled by the time it starts.
    /// </para>
    /// </remarks>
    public static void ScheduleInSeparateTurn(this IGrainBase grain, Func<Task> work)
    {
        IGrainTimer? timer = null;
        timer = grain.RegisterGrainTimer(
            async _ =>
            {
                timer?.Dispose();
                await work();
            },
            new GrainTimerCreationOptions { DueTime = TimeSpan.Zero, Period = Timeout.InfiniteTimeSpan });
    }
}

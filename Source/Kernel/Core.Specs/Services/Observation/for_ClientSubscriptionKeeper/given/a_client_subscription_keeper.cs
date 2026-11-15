// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.given;

/// <summary>
/// Keeps a subscription on a <see cref="Observation.given.ManualClock"/>: nothing waits on real time, every wait the keeper
/// starts is reported, and the specification moves the clock past it.
/// </summary>
public class a_client_subscription_keeper : Specification
{
    protected const double Draw = 0.5;
    protected static readonly TimeSpan Revise = TimeSpan.FromSeconds(2);
    protected static readonly TimeSpan SafetyNet = TimeSpan.FromSeconds(60);
    protected static readonly TimeSpan Settle = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The safety-net wait with the draw the schedule is given: between three quarters of the interval and all of it.
    /// </summary>
    protected static readonly TimeSpan SafetyNetWait = SafetyNet * (SubscriptionCheckSchedule.SafetyNetFloor + ((1 - SubscriptionCheckSchedule.SafetyNetFloor) * Draw));

    /// <summary>
    /// The spread before the first check after a departure, with the draw the schedule is given.
    /// </summary>
    protected static readonly TimeSpan Spread = Revise * Draw;

    protected Observation.given.ManualClock _clock;
    protected int _checks;
    protected TaskCompletionSource _siloLeft = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected ISiloDepartures _siloDepartures;
    protected readonly CancellationTokenSource _streamClosed = new();
    protected Task _keeping = Task.CompletedTask;

    void Establish()
    {
        _clock = new();
        _siloDepartures = Substitute.For<ISiloDepartures>();
        _siloDepartures.Next.Returns(_ => _siloLeft.Task);
    }

    async Task Destroy()
    {
        await _streamClosed.CancelAsync();
        await Observation.given.ManualClock.Within(_keeping);
        _streamClosed.Dispose();
    }

    /// <summary>
    /// Performs one check.
    /// </summary>
    /// <param name="check">The number of the check, starting at 1.</param>
    /// <returns>Whether the subscription was found intact.</returns>
    protected virtual Task<bool> Check(int check) => Task.FromResult(false);

    protected void StartKeeping() => _keeping = ClientSubscriptionKeeper.KeepWhileConnected(
        () => Check(Interlocked.Increment(ref _checks)),
        new SubscriptionCheckSchedule(Revise, SafetyNet, Settle, () => Draw),
        _siloDepartures,
        _clock,
        "the-client",
        NullLogger.Instance,
        _streamClosed.Token);

    /// <summary>
    /// Signals that a silo left, the way <see cref="SiloDepartures"/> does: the task handed out completes and a new one
    /// takes its place.
    /// </summary>
    protected void SiloLeaves()
    {
        var left = _siloLeft;
        _siloLeft = new(TaskCreationOptions.RunContinuationsAsynchronously);
        left.SetResult();
    }

    /// <summary>
    /// Lets waits elapse until the clock has moved at least as far as asked.
    /// </summary>
    /// <param name="duration">How far.</param>
    /// <returns>The waits that elapsed.</returns>
    protected async Task<List<TimeSpan>> ElapseAtLeast(TimeSpan duration)
    {
        var waits = new List<TimeSpan>();
        var elapsed = TimeSpan.Zero;
        while (elapsed < duration)
        {
            var wait = await _clock.Elapse();
            waits.Add(wait);
            elapsed += wait;
        }

        return waits;
    }
}

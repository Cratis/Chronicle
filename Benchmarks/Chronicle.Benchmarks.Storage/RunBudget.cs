// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// Bounds the whole harness run with a monotonic clock and cooperative cancellation.
/// </summary>
public sealed class RunBudget : IDisposable
{
    readonly TimeSpan _budget;
    readonly TimeProvider _time;
    readonly long _started;
    readonly CancellationTokenSource _cancellation;

    /// <summary>
    /// Initializes a budget, using the system clock unless a clock is supplied.
    /// </summary>
    /// <param name="budget">The duration, or infinite for an unbounded run.</param>
    /// <param name="time">The clock.</param>
    public RunBudget(TimeSpan budget, TimeProvider? time = null)
    {
        _budget = budget;
        _time = time ?? TimeProvider.System;
        _started = _time.GetTimestamp();
        _cancellation = new(budget, _time);
    }

    /// <summary>
    /// Gets or sets the harness budget. Outside a harness run it is unlimited.
    /// </summary>
    public static RunBudget Current { get; set; } = new(Timeout.InfiniteTimeSpan);

    /// <summary>
    /// Gets the cancellation token, including expiry observed through the clock.
    /// </summary>
    public CancellationToken Token
    {
        get
        {
            if (Expired) _cancellation.Cancel();
            return _cancellation.Token;
        }
    }

    /// <summary>
    /// Gets whether the budget has run out.
    /// </summary>
    public bool Expired => _cancellation.IsCancellationRequested || Remaining == TimeSpan.Zero;

    /// <summary>
    /// Gets the remaining duration, clamped to zero, or infinite for an unlimited run.
    /// </summary>
    public TimeSpan Remaining => _budget == Timeout.InfiniteTimeSpan ? Timeout.InfiniteTimeSpan : TimeSpan.FromTicks(Math.Max(0, (_budget - _time.GetElapsedTime(_started)).Ticks));

    /// <summary>
    /// Stops work when the profile budget has run out.
    /// </summary>
    /// <exception cref="BudgetExceededException">The profile budget is exhausted.</exception>
    public void ThrowIfExpired()
    {
        if (Expired) throw new BudgetExceededException(_budget);
    }

    /// <inheritdoc/>
    public void Dispose() => _cancellation.Dispose();
}

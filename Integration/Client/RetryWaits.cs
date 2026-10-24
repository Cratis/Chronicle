// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Timeouts for specs that wait across a failed partition being retried.
/// </summary>
public static class RetryWaits
{
    static readonly TimeSpan _minimum = TimeSpan.FromSeconds(70);

    /// <summary>
    /// Gets the bound for any wait that spans a failed-partition retry.
    /// </summary>
    /// <remarks>
    /// A retry is by design slower than the generic spec timeout. Each retry waits an exponential backoff of
    /// 1 s * 2^attempt (2 s, then 4 s; see Source/Kernel/Core/Configuration/Observers.cs), plus persisting the retry
    /// reminder and starting the retry job. When the first reminder tick is missed, the retry fires one reminder
    /// period later, and that period has a floor of 1 minute (see Source/Kernel/Core/Observation/Observer.Failing.cs).
    /// The specs wait on signals, so a generous bound costs nothing when they succeed. Use it for the waits
    /// after a failure, and nowhere else.
    /// </remarks>
    /// <returns>The larger of <see cref="TimeSpanFactory.DefaultTimeout"/> and 70 seconds.</returns>
    public static TimeSpan Timeout
    {
        get
        {
            var defaultTimeout = TimeSpanFactory.DefaultTimeout();
            return defaultTimeout == System.Threading.Timeout.InfiniteTimeSpan || defaultTimeout > _minimum
                ? defaultTimeout
                : _minimum;
        }
    }

    /// <summary>
    /// Waits until a condition holds, bounded by <see cref="Timeout"/>.
    /// </summary>
    /// <param name="condition">The condition to poll.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task Until(Func<Task<bool>> condition)
    {
        using var cts = new CancellationTokenSource(Timeout);
        while (!await condition())
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50), cts.Token);
        }
    }
}

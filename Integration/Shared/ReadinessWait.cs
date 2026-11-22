// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Cratis.Chronicle;

/// <summary>
/// Bounded observation of eventually consistent integration state.
/// </summary>
internal static class ReadinessWait
{
    public static async Task<T> Until<T>(
        Func<Task<T>> observe,
        Func<T, bool> ready,
        TimeSpan? timeout = null,
        [CallerArgumentExpression(nameof(observe))] string condition = "",
        [CallerArgumentExpression(nameof(ready))] string expectation = "",
        TimeSpan? pollInterval = null)
    {
        var bound = timeout ?? TimeSpanFactory.DefaultTimeout();
        using var deadline = new CancellationTokenSource(bound);
        var elapsed = Stopwatch.StartNew();
        Func<string> lastObserved = () => "<no observation completed>";
        try
        {
            while (true)
            {
                var value = await observe().WaitAsync(deadline.Token);
                lastObserved = () => Describe(value);
                if (ready(value))
                {
                    return value;
                }

                await Backoff(pollInterval, deadline.Token);
            }
        }
        catch (OperationCanceledException exception) when (deadline.IsCancellationRequested)
        {
            throw new TimeoutException($"Timed out after {elapsed.Elapsed} waiting for {condition} to satisfy {expectation}. Last observed: {lastObserved()}.", exception);
        }
    }

    public static async Task<T> ObserveFor<T>(Func<Task<T>> observe, TimeSpan duration)
    {
        using var deadline = new CancellationTokenSource(duration);
        T lastObserved = default!;
        var observed = false;
        try
        {
            while (true)
            {
                lastObserved = await observe().WaitAsync(deadline.Token);
                observed = true;
                await Backoff(null, deadline.Token);
            }
        }
        catch (OperationCanceledException exception) when (deadline.IsCancellationRequested)
        {
            if (!observed)
            {
                throw new TimeoutException($"No state was observed within {duration}.", exception);
            }
            return lastObserved;
        }
    }

    static Task Backoff(TimeSpan? pollInterval, CancellationToken cancellationToken)
    {
        // Task.Delay: bounded backoff between observations of eventually consistent state.
        return Task.Delay(pollInterval ?? TimeSpan.FromMilliseconds(50), cancellationToken);
    }

    static string Describe<T>(T value)
    {
        try
        {
            return JsonSerializer.Serialize(value);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return $"{value} (JSON unavailable: {exception.Message})";
        }
    }
}

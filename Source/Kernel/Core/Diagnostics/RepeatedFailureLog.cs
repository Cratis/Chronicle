// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Decides which of a stream of failures to write, so one repeated cause is one fact and not one stack trace each.
/// </summary>
/// <remarks>
/// A restarted silo with thousands of grains failing for the same reason wrote about 8,000 lines a second, which cost more
/// than the failures and left the operator the least useful sample of the incident (#5678). A failure is the same one when it comes
/// from the same logger category with the same kind of exception at its root. The first few in each window are written in full,
/// the rest are counted and reported in one line when the window ends.
/// </remarks>
/// <param name="window">How long counting lasts before the suppressed failures are reported.</param>
/// <param name="allowedPerWindow">How many failures of a kind are written in full in each window.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> deciding what time it is.</param>
public sealed class RepeatedFailureLog(TimeSpan window, int allowedPerWindow, TimeProvider timeProvider)
{
    /// <summary>
    /// The default number of failures written in full for each kind in each window.
    /// </summary>
    public const int DefaultAllowedPerWindow = 3;

    /// <summary>
    /// The default window.
    /// </summary>
    public static readonly TimeSpan DefaultWindow = TimeSpan.FromSeconds(10);

    readonly ConcurrentDictionary<(string Category, Type Root), Counter> _counters = new();

    /// <summary>
    /// Decides whether to write a log entry.
    /// </summary>
    /// <param name="category">The logger category.</param>
    /// <param name="level">The level of the entry.</param>
    /// <param name="exception">The exception of the entry, if any.</param>
    /// <returns>True if the entry is written, false if it is counted instead.</returns>
    public bool ShouldWrite(string category, LogLevel level, Exception? exception)
    {
        if (exception is null || level < LogLevel.Warning)
        {
            return true;
        }

        var counter = _counters.GetOrAdd((category, Root(exception)), _ => new());
        lock (counter)
        {
            var now = timeProvider.GetUtcNow();
            if (counter.Started == default || now - counter.Started >= window)
            {
                counter.Started = now;
                counter.Written = 0;
            }

            if (counter.Written < allowedPerWindow)
            {
                counter.Written++;
                return true;
            }

            counter.Suppressed++;
            return false;
        }
    }

    /// <summary>
    /// Takes the counts of failures that were suppressed in windows that have ended.
    /// </summary>
    /// <returns>One entry for each kind of failure with its count.</returns>
    public IReadOnlyList<(string Category, Type Exception, int Suppressed)> TakeEnded()
    {
        var ended = new List<(string, Type, int)>();
        var now = timeProvider.GetUtcNow();
        foreach (var ((category, root), counter) in _counters)
        {
            lock (counter)
            {
                if (counter.Suppressed > 0 && now - counter.Started >= window)
                {
                    ended.Add((category, root, counter.Suppressed));
                    counter.Suppressed = 0;
                }
            }
        }

        return ended;
    }

    static Type Root(Exception exception)
    {
        while (exception.InnerException is { } inner)
        {
            exception = inner;
        }

        return exception.GetType();
    }

    sealed class Counter
    {
        public DateTimeOffset Started { get; set; }

        public int Written { get; set; }

        public int Suppressed { get; set; }
    }
}

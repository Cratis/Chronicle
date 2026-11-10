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
/// from the same logger category, with the same message template and the same kind of exception at its root. The first ones in
/// each window are written in full, the rest are counted and reported in one line when the window ends. A failure that differs
/// in any of those is a different one, and its first occurrence is always written.
/// <para>
/// The number of kinds tracked at once is bounded. Kinds that stop failing are forgotten once their window has ended and been
/// reported; when the bound is still reached, any further kind is counted under one shared overflow kind.
/// </para>
/// </remarks>
/// <param name="window">How long counting lasts before the suppressed failures are reported.</param>
/// <param name="allowedPerWindow">How many failures of a kind are written in full in each window.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> deciding what time it is.</param>
/// <param name="maxKinds">The most kinds of failures tracked at once.</param>
public sealed class RepeatedFailureLog(TimeSpan window, int allowedPerWindow, TimeProvider timeProvider, int maxKinds = RepeatedFailureLog.DefaultMaxKinds)
{
    /// <summary>
    /// The default number of failures written in full for each kind in each window.
    /// </summary>
    public const int DefaultAllowedPerWindow = 1;

    /// <summary>
    /// The default most kinds of failures tracked at once.
    /// </summary>
    public const int DefaultMaxKinds = 1024;

    /// <summary>
    /// The category the failures are counted under when more kinds fail than are tracked.
    /// </summary>
    public const string OverflowCategory = "(other failures)";

    /// <summary>
    /// The default window.
    /// </summary>
    public static readonly TimeSpan DefaultWindow = TimeSpan.FromSeconds(10);

    static readonly Kind _overflow = new(OverflowCategory, string.Empty, typeof(Exception));

    readonly ConcurrentDictionary<Kind, Counter> _counters = new();
    int _kinds;

    /// <summary>
    /// Gets the window counting lasts for.
    /// </summary>
    public TimeSpan Window => window;

    /// <summary>
    /// Decides whether to write a log entry.
    /// </summary>
    /// <param name="category">The logger category.</param>
    /// <param name="level">The level of the entry.</param>
    /// <param name="exception">The exception of the entry, if any.</param>
    /// <returns>True if the entry is written, false if it is counted instead.</returns>
    public bool ShouldWrite(string category, LogLevel level, Exception? exception) => ShouldWrite(category, string.Empty, level, exception);

    /// <summary>
    /// Decides whether to write a log entry.
    /// </summary>
    /// <param name="category">The logger category.</param>
    /// <param name="message">The message template of the entry, telling different failures in the same category apart.</param>
    /// <param name="level">The level of the entry.</param>
    /// <param name="exception">The exception of the entry, if any.</param>
    /// <returns>True if the entry is written, false if it is counted instead.</returns>
    public bool ShouldWrite(string category, string message, LogLevel level, Exception? exception)
    {
        if (exception is null || level < LogLevel.Warning)
        {
            return true;
        }

        var kind = new Kind(category, message, Root(exception));
        while (true)
        {
            var counter = CounterFor(kind);
            lock (counter)
            {
                if (counter.Removed)
                {
                    continue;
                }

                var now = timeProvider.GetUtcNow();
                if (counter.Started == default || now - counter.Started >= window)
                {
                    counter.Started = now;
                    counter.Written = 0;
                    counter.Ended += counter.Suppressed;
                    counter.Suppressed = 0;
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
    }

    /// <summary>
    /// Takes the counts of failures that were suppressed in windows that have ended, and forgets kinds that stopped failing.
    /// </summary>
    /// <returns>One entry for each kind of failure with its count.</returns>
    public IReadOnlyList<SuppressedFailures> TakeEnded()
    {
        var ended = new List<SuppressedFailures>();
        var now = timeProvider.GetUtcNow();
        foreach (var (kind, counter) in _counters)
        {
            lock (counter)
            {
                var windowEnded = now - counter.Started >= window;
                var suppressed = counter.Ended + (windowEnded ? counter.Suppressed : 0);
                if (suppressed > 0)
                {
                    ended.Add(new(kind.Category, kind.Message, kind.Exception, suppressed));
                    counter.Ended = 0;
                    if (windowEnded)
                    {
                        counter.Suppressed = 0;
                    }
                }
                else if (windowEnded && _counters.TryRemove(new KeyValuePair<Kind, Counter>(kind, counter)))
                {
                    counter.Removed = true;
                    Interlocked.Decrement(ref _kinds);
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

    Counter CounterFor(Kind kind)
    {
        while (true)
        {
            if (_counters.TryGetValue(kind, out var counter))
            {
                return counter;
            }

            if (Volatile.Read(ref _kinds) >= maxKinds)
            {
                kind = _overflow;
                if (_counters.TryGetValue(kind, out counter))
                {
                    return counter;
                }
            }

            counter = new();
            if (_counters.TryAdd(kind, counter))
            {
                Interlocked.Increment(ref _kinds);
                return counter;
            }
        }
    }

    readonly record struct Kind(string Category, string Message, Type Exception);

    sealed class Counter
    {
        public DateTimeOffset Started { get; set; }

        public int Written { get; set; }

        public int Suppressed { get; set; }

        public int Ended { get; set; }

        public bool Removed { get; set; }
    }
}

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
/// in any of those is a different one, and while fewer than the bound of kinds are tracked its first occurrence is always written.
/// <para>
/// The number of kinds tracked at once is bounded, and never exceeds the bound even when new kinds fail concurrently. Kinds that
/// stop failing are forgotten once their window has ended and been reported. While the bound is reached, failures of kinds that
/// are not tracked share one budget instead: the first ones of them in each window are written in full whatever their kind, and
/// the rest are counted by the type of the exception at their root and reported under <see cref="OverflowCategory"/>. The first
/// occurrence of an untracked kind is therefore only written when that shared budget is not spent.
/// </para>
/// </remarks>
/// <param name="window">How long counting lasts before the suppressed failures are reported.</param>
/// <param name="allowedPerWindow">How many failures of a kind are written in full in each window.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> deciding what time it is.</param>
/// <param name="maxKinds">The most kinds of failures tracked at once.</param>
/// <param name="untrackedAllowedPerWindow">How many failures of kinds that are not tracked are written in full in each window, together.</param>
public sealed class RepeatedFailureLog(
    TimeSpan window,
    int allowedPerWindow,
    TimeProvider timeProvider,
    int maxKinds = RepeatedFailureLog.DefaultMaxKinds,
    int untrackedAllowedPerWindow = RepeatedFailureLog.DefaultUntrackedAllowedPerWindow)
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
    /// The default number of failures of kinds that are not tracked written in full in each window, together.
    /// </summary>
    public const int DefaultUntrackedAllowedPerWindow = 10;

    /// <summary>
    /// The most exception types failures of untracked kinds are counted by; any further type is counted as <see cref="Exception"/>.
    /// </summary>
    public const int MaxUntrackedExceptionTypes = 64;

    /// <summary>
    /// The category the failures are counted under when more kinds fail than are tracked.
    /// </summary>
    public const string OverflowCategory = "(other failures)";

    /// <summary>
    /// The default window.
    /// </summary>
    public static readonly TimeSpan DefaultWindow = TimeSpan.FromSeconds(10);

    readonly ConcurrentDictionary<Kind, Counter> _counters = new();
    readonly Untracked _untracked = new();
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

        var root = Root(exception);
        var kind = new Kind(category, message, root);
        while (true)
        {
            var counter = CounterFor(kind);
            if (counter is null)
            {
                return ShouldWriteUntracked(root, level);
            }

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
                    counter.EndWindow();
                }

                if (counter.Written < allowedPerWindow)
                {
                    counter.Written++;
                    return true;
                }

                counter.Count(level);
                return false;
            }
        }
    }

    /// <summary>
    /// Takes the counts of failures that were suppressed in windows that have ended, and forgets kinds that stopped failing.
    /// </summary>
    /// <param name="includeRunningWindows">Whether to also take the counts of windows that are still running, for when nothing will report them later, such as at shutdown.</param>
    /// <returns>One entry for each kind of failure with its count.</returns>
    public IReadOnlyList<SuppressedFailures> TakeEnded(bool includeRunningWindows = false)
    {
        var ended = new List<SuppressedFailures>();
        var now = timeProvider.GetUtcNow();
        foreach (var (kind, counter) in _counters)
        {
            lock (counter)
            {
                var windowEnded = now - counter.Started >= window;
                if (counter.Take(windowEnded || includeRunningWindows) is { } taken)
                {
                    ended.Add(new(kind.Category, kind.Message, kind.Exception, taken.Suppressed, taken.Level));
                }
                else if (windowEnded && _counters.TryRemove(new KeyValuePair<Kind, Counter>(kind, counter)))
                {
                    counter.Removed = true;
                    Interlocked.Decrement(ref _kinds);
                }
            }
        }

        lock (_untracked)
        {
            var windowEnded = now - _untracked.Started >= window;
            foreach (var (exception, counter) in _untracked.ByException.ToArray())
            {
                if (counter.Take(windowEnded || includeRunningWindows) is { } taken)
                {
                    ended.Add(new(OverflowCategory, string.Empty, exception, taken.Suppressed, taken.Level));
                }
                else if (windowEnded)
                {
                    _untracked.ByException.Remove(exception);
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

    static LogLevel Max(LogLevel left, LogLevel right) => left > right ? left : right;

    bool ShouldWriteUntracked(Type exception, LogLevel level)
    {
        lock (_untracked)
        {
            var now = timeProvider.GetUtcNow();
            if (_untracked.Started == default || now - _untracked.Started >= window)
            {
                _untracked.Started = now;
                _untracked.Written = 0;
                foreach (var counter in _untracked.ByException.Values)
                {
                    counter.EndWindow();
                }
            }

            if (_untracked.Written < untrackedAllowedPerWindow)
            {
                _untracked.Written++;
                return true;
            }

            if (!_untracked.ByException.ContainsKey(exception) && _untracked.ByException.Count >= MaxUntrackedExceptionTypes)
            {
                exception = typeof(Exception);
            }

            if (!_untracked.ByException.TryGetValue(exception, out var byException))
            {
                byException = new();
                _untracked.ByException[exception] = byException;
            }

            byException.Count(level);
            return false;
        }
    }

    Counter? CounterFor(Kind kind)
    {
        while (true)
        {
            if (_counters.TryGetValue(kind, out var counter))
            {
                return counter;
            }

            // Reserve the place before adding, so concurrent new kinds can never take the count beyond the bound.
            if (Interlocked.Increment(ref _kinds) > maxKinds)
            {
                Interlocked.Decrement(ref _kinds);
                return null;
            }

            counter = new();
            if (_counters.TryAdd(kind, counter))
            {
                return counter;
            }

            Interlocked.Decrement(ref _kinds);
        }
    }

    readonly record struct Kind(string Category, string Message, Type Exception);

    sealed class Counter
    {
        public DateTimeOffset Started { get; set; }

        public int Written { get; set; }

        public bool Removed { get; set; }

        int Suppressed { get; set; }

        LogLevel Level { get; set; }

        int Ended { get; set; }

        LogLevel EndedLevel { get; set; }

        public void Count(LogLevel level)
        {
            Suppressed++;
            Level = Max(Level, level);
        }

        public void EndWindow()
        {
            Ended += Suppressed;
            EndedLevel = Max(EndedLevel, Level);
            Suppressed = 0;
            Level = LogLevel.Trace;
        }

        public (int Suppressed, LogLevel Level)? Take(bool includeRunning)
        {
            var suppressed = Ended + (includeRunning ? Suppressed : 0);
            if (suppressed == 0)
            {
                return null;
            }

            var level = includeRunning ? Max(EndedLevel, Level) : EndedLevel;
            Ended = 0;
            EndedLevel = LogLevel.Trace;
            if (includeRunning)
            {
                Suppressed = 0;
                Level = LogLevel.Trace;
            }

            return (suppressed, level);
        }
    }

    sealed class Untracked
    {
        public DateTimeOffset Started { get; set; }

        public int Written { get; set; }

        public Dictionary<Type, Counter> ByException { get; } = [];
    }
}

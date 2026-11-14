// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Limits how many requests one source may make to the enrollment routes, which are the only pre-authentication surface of the federation listener.
/// </summary>
/// <remarks>
/// A fixed one-minute window per source. IPv6 sources are grouped by their /64 prefix, because a single host is typically handed a whole /64.
/// Memory is bounded: when too many sources are tracked, expired windows are dropped, and if that is not enough the oldest tenth are dropped.
/// A full table never refuses everyone, because the routes are reachable before authentication and one flood must not lock the others out.
/// Each <see cref="FederationEnrollmentBudget"/> has its own window, so a flood of one kind of request does not use up the other.
/// </remarks>
/// <param name="timeProvider">The <see cref="TimeProvider"/> used to open and close windows.</param>
[Singleton]
public class FederationEnrollmentRateLimiter(TimeProvider timeProvider)
{
    const int MaxTrackedSources = 10_000;
    static readonly TimeSpan _window = TimeSpan.FromMinutes(1);
    readonly ConcurrentDictionary<string, Window> _windows = new(StringComparer.Ordinal);
    int _inFlight;

    /// <summary>
    /// Try to admit a request from a source.
    /// </summary>
    /// <param name="source">The address the request came from.</param>
    /// <param name="requestsPerMinute">How many requests a source may make per minute.</param>
    /// <param name="budget">Which budget the request counts against.</param>
    /// <returns>True when the request is admitted, false when the source must be refused.</returns>
    public bool TryAcquire(IPAddress? source, int requestsPerMinute, FederationEnrollmentBudget budget = FederationEnrollmentBudget.Discovery)
    {
        var key = $"{budget}|{Key(source)}";
        var now = timeProvider.GetTimestamp();

        if (_windows.Count >= MaxTrackedSources && !_windows.ContainsKey(key))
        {
            Evict(now);
            if (_windows.Count >= MaxTrackedSources)
            {
                EvictOldest();
            }
        }

        var window = _windows.GetOrAdd(key, static (_, started) => new Window(started), now);
        lock (window)
        {
            if (timeProvider.GetElapsedTime(window.Started, now) >= _window)
            {
                window.Started = now;
                window.Count = 0;
            }

            window.Count++;
            return window.Count <= requestsPerMinute;
        }
    }

    /// <summary>
    /// Try to start serving a request, so that no more than <paramref name="maxConcurrent"/> are being served at once.
    /// </summary>
    /// <param name="maxConcurrent">How many requests may be served at once.</param>
    /// <returns>A lease to dispose when the request has been served, or null when the route is at capacity and the request must be refused.</returns>
    public IDisposable? TryEnter(int maxConcurrent)
    {
        if (Interlocked.Increment(ref _inFlight) > maxConcurrent)
        {
            Interlocked.Decrement(ref _inFlight);
            return null;
        }

        return new Lease(this);
    }

    static string Key(IPAddress? source)
    {
        if (source is null)
        {
            return "unknown";
        }

        if (source.IsIPv4MappedToIPv6)
        {
            source = source.MapToIPv4();
        }

        if (source.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = source.GetAddressBytes();
            return new IPAddress(bytes.AsSpan(0, 8).ToArray().Concat(new byte[8]).ToArray()).ToString();
        }

        return source.ToString();
    }

    void EvictOldest()
    {
        var oldest = _windows
            .Select(_ =>
            {
                lock (_.Value)
                {
                    return (_.Key, _.Value.Started);
                }
            })
            .OrderBy(_ => _.Started)
            .Take(MaxTrackedSources / 10)
            .ToArray();

        foreach (var (key, _) in oldest)
        {
            _windows.TryRemove(key, out _);
        }
    }

    void Evict(long now)
    {
        foreach (var (key, window) in _windows)
        {
            bool expired;
            lock (window)
            {
                expired = timeProvider.GetElapsedTime(window.Started, now) >= _window;
            }

            if (expired)
            {
                _windows.TryRemove(key, out _);
            }
        }
    }

    sealed class Lease(FederationEnrollmentRateLimiter limiter) : IDisposable
    {
        int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                Interlocked.Decrement(ref limiter._inFlight);
            }
        }
    }

    sealed class Window(long started)
    {
        public long Started { get; set; } = started;

        public int Count { get; set; }
    }
}

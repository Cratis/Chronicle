// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Reacts to benchmark events by incrementing an in-memory counter.
/// </summary>
public class BenchmarkReactor : IReactor
{
    static int _totalEventsProcessed;

    int _eventsProcessed;

    /// <summary>
    /// Gets the process-wide number of benchmark events delivered. The reactor is activated per handling scope,
    /// so only a process-wide count reflects deliveries.
    /// </summary>
    public static int TotalEventsProcessed => Volatile.Read(ref _totalEventsProcessed);

    /// <summary>
    /// Gets the number of benchmark events processed by the reactor.
    /// </summary>
    public int EventsProcessed => _eventsProcessed;

    /// <summary>
    /// Handles a benchmark event.
    /// </summary>
    /// <param name="evt">The event being handled.</param>
    /// <param name="context">The event context.</param>
    /// <returns>A completed task.</returns>
    public Task OnBenchmarkEvent(BenchmarkEvent evt, EventContext context)
    {
        _ = evt;
        _ = context;

        Interlocked.Increment(ref _eventsProcessed);
        Interlocked.Increment(ref _totalEventsProcessed);
        return Task.CompletedTask;
    }
}

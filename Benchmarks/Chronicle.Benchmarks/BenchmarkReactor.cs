// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Chronicle.Benchmarks;

public class BenchmarkReactor : IReactor
{
    int _eventsProcessed;

    public int EventsProcessed => _eventsProcessed;

    public Task OnBenchmarkEvent(BenchmarkEvent evt, EventContext context)
    {
        Interlocked.Increment(ref _eventsProcessed);
        return Task.CompletedTask;
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>Tracks first observed marker visibility on one read path.</summary>
/// <param name="read">The read measurement input.</param>
/// <param name="clock">The clock measurement input.</param>
/// <param name="acknowledgements">The acknowledgements measurement input.</param>
internal sealed class RebuildBaselineProbe(Func<Task<int>> read, Stopwatch clock, ConcurrentQueue<double> acknowledgements)
{
    readonly Dictionary<int, double> _observed = [];

    /// <summary>Samples acknowledged marker visibility.</summary>
    /// <param name="recordObserved">Whether this is a continuous sample rather than the final censoring sample.</param>
    /// <param name="cancellationToken">Cancellation of the read wait.</param>
    /// <returns>A task that completes after sampling newly visible marker revisions.</returns>
    internal async Task Sample(bool recordObserved = true, CancellationToken cancellationToken = default)
    {
        var submitted = acknowledgements.ToArray();
        var count = submitted.Length;
        var revision = await read().WaitAsync(cancellationToken);
        if (!recordObserved)
        {
            return;
        }
        var now = clock.Elapsed.TotalSeconds;
        for (var index = 0; index < Math.Min(count, revision); index++)
        {
            _observed.TryAdd(index, (now - submitted[index]) * 1000);
        }
    }

    /// <summary>Reports observed and right-censored marker latencies.</summary>
    /// <param name="timedOut">The timedOut measurement input.</param>
    /// <returns>The observed latency statistics and right-censored marker lower bounds.</returns>
    internal object Report(bool timedOut) => RebuildBaselineMath.Latencies(acknowledgements.ToArray(), _observed, clock.Elapsed.TotalSeconds, timedOut);
}

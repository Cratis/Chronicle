// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Samples one read path against acknowledged marker appends, without assuming both paths serve the same container.
/// </summary>
/// <param name="read">Reads the current marker revision, or -1 for an absent marker.</param>
/// <param name="clock">The shared measurement clock.</param>
/// <param name="acknowledgements">Append acknowledgement times, in revision order (starting at revision one).</param>
internal sealed class RebuildReadProbe(
    Func<Task<int>> read,
    Stopwatch clock,
    ConcurrentQueue<double> acknowledgements)
{
    readonly List<object> _samples = [];
    int _visibleRevision;
    int _reads;
    int _missingReads;
    int _regressions;
    int _maximumRevisionLag;
    double _maximumPendingAgeMs;
    double _maximumVisibilityLatencyMs;

    /// <summary>
    /// Gets the most recently sampled nonnegative marker revision.
    /// </summary>
    internal int VisibleRevision => _visibleRevision;

    /// <summary>
    /// Samples visibility against the acknowledged fence captured before the read.
    /// </summary>
    /// <param name="phase">The rebuild phase to associate with the sample.</param>
    /// <returns>A task representing the sample operation.</returns>
    internal async Task Sample(string phase)
    {
        // Capture the acknowledged fence before issuing the read, so an append racing the read
        // cannot be incorrectly counted as stale. Calls on a given probe are sequential.
        var acknowledged = acknowledgements.ToArray();
        var revision = await read();
        var now = clock.Elapsed.TotalSeconds;
        _reads++;
        if (revision < 0) _missingReads++;
        if (revision < _visibleRevision) _regressions++;
        var lag = Math.Max(0, acknowledged.Length - Math.Max(0, revision));
        var pendingAgeMs = lag > 0 ? (now - acknowledged[Math.Max(0, revision)]) * 1000 : 0;
        var visible = Math.Min(revision, acknowledged.Length);
        for (var index = _visibleRevision; index < visible; index++)
        {
            _maximumVisibilityLatencyMs = Math.Max(_maximumVisibilityLatencyMs, (now - acknowledged[index]) * 1000);
        }
        _visibleRevision = Math.Max(0, revision);
        _maximumRevisionLag = Math.Max(_maximumRevisionLag, lag);
        _maximumPendingAgeMs = Math.Max(_maximumPendingAgeMs, pendingAgeMs);
        _samples.Add(new { AtSeconds = now, Phase = phase, Revision = revision, Acknowledged = acknowledged.Length, Lag = lag, PendingAgeMs = pendingAgeMs });
    }

    /// <summary>
    /// Reports the accumulated visibility measurements and timestamped samples.
    /// </summary>
    /// <returns>The measurements for JSON serialization.</returns>
    internal object Report() => new
    {
        Reads = _reads,
        MissingReads = _missingReads,
        Regressions = _regressions,
        MaximumRevisionLag = _maximumRevisionLag,
        MaximumPendingAgeMs = _maximumPendingAgeMs,
        MaximumVisibilityLatencyMs = _maximumVisibilityLatencyMs,
        Samples = _samples
    };
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>Pure fence, throughput and right-censored latency calculations.</summary>
internal static class RebuildBaselineMath
{
    /// <summary>Calculates subscribed seed events per request-to-coverage second.</summary>
    /// <param name="subscribedEvents">The subscribedEvents measurement input.</param>
    /// <param name="requestSeconds">The requestSeconds measurement input.</param>
    /// <param name="coverageSeconds">The coverageSeconds measurement input.</param>
    /// <returns>The subscribed seed events processed per second.</returns>
    /// <exception cref="ArgumentException">The corpus is empty or the coverage interval is not positive.</exception>
    internal static double Throughput(long subscribedEvents, double requestSeconds, double coverageSeconds)
    {
        if (subscribedEvents <= 0 || coverageSeconds <= requestSeconds) throw new ArgumentException("A nonempty corpus and a positive coverage interval are required.");
        return subscribedEvents / (coverageSeconds - requestSeconds);
    }

    /// <summary>Calculates nearest-rank percentiles from observed latencies only.</summary>
    /// <param name="observed">The observed measurement input.</param>
    /// <param name="percentile">The percentile measurement input.</param>
    /// <returns>The nearest-rank observed percentile, or null for an empty sample.</returns>
    internal static double? Percentile(IEnumerable<double> observed, double percentile)
    {
        var sorted = observed.Order().ToArray();
        return sorted.Length == 0 ? null : sorted[Math.Clamp((int)Math.Ceiling(percentile * sorted.Length) - 1, 0, sorted.Length - 1)];
    }

    /// <summary>Reports observed percentiles and lower bounds for censored markers.</summary>
    /// <param name="submitted">The submitted measurement input.</param>
    /// <param name="observed">The observed measurement input.</param>
    /// <param name="now">The now measurement input.</param>
    /// <param name="timedOut">The timedOut measurement input.</param>
    /// <returns>Observed latency statistics and lower bounds for unobserved markers.</returns>
    internal static object Latencies(IReadOnlyList<double> submitted, IReadOnlyDictionary<int, double> observed, double now, bool timedOut) => new
    {
        Submitted = submitted.Count,
        Observed = observed.Count,
        Pending = submitted.Count - observed.Count,
        TimedOut = timedOut ? submitted.Count - observed.Count : 0,
        P50Ms = Percentile(observed.Values, .5),
        P99Ms = Percentile(observed.Values, .99),
        MaxMs = Percentile(observed.Values, 1),
        Censored = submitted.Select((at, index) => new { Index = index, ElapsedLowerBoundMs = Math.Max(0, (now - at) * 1000) }).Where(item => !observed.ContainsKey(item.Index)).ToArray()
    };
}

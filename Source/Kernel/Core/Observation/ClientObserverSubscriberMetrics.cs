// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Records bounded-cardinality client-delivery fence measurements.
/// </summary>
internal static class ClientObserverSubscriberMetrics
{
    static readonly Meter _meter = new(WellKnown.MeterName);
    static readonly Counter<long> _lateResults = _meter.CreateCounter<long>("abandoned-delivery-late-results", description: "Client results received after the delivery deadline");
    static readonly Counter<long> _holdExpirations = _meter.CreateCounter<long>("abandoned-delivery-hold-expirations", description: "Client deliveries released by the maximum hold");
    static readonly Histogram<double> _fenceDuration = _meter.CreateHistogram<double>("abandoned-delivery-fence-duration", unit: "s", description: "Time an abandoned client delivery remained held");

    /// <summary>Counts a late result by state.</summary>
    /// <param name="state">The result state.</param>
    internal static void LateResult(ObserverSubscriberState state) => _lateResults.Add(1, new KeyValuePair<string, object?>("state", state.ToString()));

    /// <summary>Counts a maximum-hold expiration.</summary>
    internal static void HoldExpired() => _holdExpirations.Add(1);

    /// <summary>Records the actual hold duration.</summary>
    /// <param name="duration">The elapsed hold.</param>
    internal static void FenceDuration(TimeSpan duration) => _fenceDuration.Record(Math.Max(0, duration.TotalSeconds));
}

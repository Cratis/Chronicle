// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Metrics;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_metrics : an_observer_with_subscription
{
    protected const string PartitionsFailed = "chronicle-observer-partitions-failed";
    protected const string PartitionRetryAttempts = "chronicle-observer-partition-retry-attempts";
    protected const string PartitionsQuarantined = "chronicle-observer-partitions-quarantined";
    protected const string ObserverQuarantined = "chronicle-observer-quarantined";

    protected static readonly string[] _failureInstruments = [PartitionsFailed, PartitionRetryAttempts, PartitionsQuarantined, ObserverQuarantined];
    protected static readonly string[] _observerScopeTags = ["EventStore", "Namespace", "ObserverId", "EventSequenceId"];

    /// <summary>
    /// The recorder of the observer's metrics. It is created as a field so that it is listening before the observer
    /// is activated, which is when the observer first reports.
    /// </summary>
    protected readonly ObserverMetricsRecorder _metrics = new();

    protected override ObserverId _observerId => _metrics.ObserverId;

    protected override IMeter<Observer> CreateMeter() => new Meter<Observer>(ObserverMetricsRecorder.SharedMeter);

    /// <summary>
    /// Gets whether an instrument has measurements and every one of them is tagged with the observer scope only.
    /// </summary>
    /// <param name="instrument">The name of the instrument.</param>
    /// <returns>True if there are measurements and all carry exactly the observer scope tags.</returns>
    protected bool IsTaggedWithObserverScopeOnly(string instrument)
    {
        var measurements = _metrics.For(instrument).ToArray();
        return measurements.Length > 0 && measurements.All(_ => _.Tags.Keys.Order().SequenceEqual(_observerScopeTags.Order()));
    }

    /// <summary>
    /// Gets whether an instrument has measurements and none of them is tagged with the partition.
    /// </summary>
    /// <param name="instrument">The name of the instrument.</param>
    /// <returns>True if there are measurements and none carries a partition tag.</returns>
    protected bool IsNotTaggedWithPartition(string instrument)
    {
        var measurements = _metrics.For(instrument).ToArray();
        return measurements.Length > 0 && !measurements.Any(_ => _.Tags.ContainsKey("partition"));
    }

    void Destroy() => _metrics.Dispose();
}

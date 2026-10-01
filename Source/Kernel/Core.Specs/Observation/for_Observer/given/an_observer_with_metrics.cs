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

    void Destroy() => _metrics.Dispose();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Metrics;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_unsubscribed_observer_with_metrics : an_observer
{
    protected readonly ObserverMetricsRecorder _metrics = new();

    protected override ObserverId _observerId => _metrics.ObserverId;

    protected override IMeter<Observer> CreateMeter() => new Meter<Observer>(ObserverMetricsRecorder.SharedMeter);

    void Destroy() => _metrics.Dispose();
}

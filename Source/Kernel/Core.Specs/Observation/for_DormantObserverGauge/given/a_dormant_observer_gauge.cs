// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Metrics;

namespace Cratis.Chronicle.Observation.for_DormantObserverGauge.given;

public class a_dormant_observer_gauge : Specification
{
    protected static readonly EventSequenceKey _firstQueue = new(EventSequenceId.Log, "store", "namespace");
    protected static readonly EventSequenceKey _otherNamespace = new(EventSequenceId.Log, "store", "other-namespace");

    protected DormantObserverGauge _gauge;
    Meter _meter;

    void Establish()
    {
        _meter = new($"dormant-gauge-{Guid.NewGuid():N}");
        var meter = Substitute.For<IMeter<DormantObserverGauge>>();
        meter.ActualMeter.Returns(_meter);
        _gauge = new(meter);
    }

    void Destroy() => _meter.Dispose();

    protected Dictionary<string, int> Observed() => _gauge.Observe().ToDictionary(
        measurement => (string)measurement.Tags.ToArray().Single(tag => tag.Key.EndsWith("namespace", StringComparison.Ordinal)).Value!,
        measurement => measurement.Value);
}

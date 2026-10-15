// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Metrics;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsGauge.given;

public class a_gauge : Specification
{
    protected SettableTimeProvider _time;
    protected Meter _meter;
    protected AlertIncidentsGauge _gauge;
    protected IAlertIncidentsGauge _holder;

    void Establish()
    {
        _time = new(new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        _meter = new($"test-{Guid.NewGuid():N}");
        var meter = Substitute.For<IMeter<AlertIncidentsGauge>>();
        meter.ActualMeter.Returns(_meter);
        _gauge = new(meter, _time);
        _holder = _gauge;
    }

    void Destroy() => _meter.Dispose();

    protected AlertIncidentsGaugeSnapshot Snapshot(long value = 1) => AlertIncidentsGaugeSnapshot.From(
        _time.GetUtcNow(),
        [(new AlertIncidentGaugeBucket("store", "Default", "observer", "event-log", "partition-failing", Concepts.Alerts.AlertSeverity.Warning), value)]);
}

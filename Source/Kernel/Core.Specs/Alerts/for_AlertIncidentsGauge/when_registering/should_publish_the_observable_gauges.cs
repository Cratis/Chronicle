// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsGauge.when_registering;

public class should_publish_the_observable_gauges : given.a_gauge
{
    readonly List<Instrument> _instruments = [];

    void Because()
    {
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, _) => { if (instrument.Meter == _meter) _instruments.Add(instrument); };
        listener.Start();
    }

    [Fact] void should_have_the_incident_gauge() => _instruments.Single(_ => _.Name == "chronicle-alerts-open-incidents").Unit.ShouldBeNull();
    [Fact] void should_have_the_availability_gauge() => _instruments.Exists(_ => _.Name == "chronicle-alerts-open-incidents-available").ShouldBeTrue();
    [Fact] void should_describe_the_incident_gauge() => _instruments.Single(_ => _.Name == "chronicle-alerts-open-incidents").Description.ShouldContain("open alert incidents");
}

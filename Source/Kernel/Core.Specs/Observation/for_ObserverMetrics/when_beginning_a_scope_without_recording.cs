// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Diagnostics;
using Cratis.Metrics;

namespace Cratis.Chronicle.Observation.for_ObserverMetrics;

public class when_beginning_a_scope_without_recording : Specification
{
    readonly System.Diagnostics.Metrics.Meter _meter = new($"scope-without-recording-{Guid.NewGuid():N}");
    readonly MeterListener _listener = new();
    readonly List<Instrument> _instruments = [];

    void Establish()
    {
        _listener.InstrumentPublished = (instrument, _) =>
        {
            if (ReferenceEquals(instrument.Meter, _meter)) _instruments.Add(instrument);
        };
        _listener.Start();
    }

    void Because() => new Meter<Observer>(_meter).BeginObserverScope("observer", new("observer", "store", "namespace", "sequence"), MetricNames.Both);

    void Destroy()
    {
        _listener.Dispose();
        _meter.Dispose();
    }

    [Fact] void should_not_create_any_instrument() => _instruments.ShouldBeEmpty();
}

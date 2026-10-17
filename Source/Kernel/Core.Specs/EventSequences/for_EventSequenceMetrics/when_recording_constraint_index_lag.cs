// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Metrics;

namespace Cratis.Chronicle.EventSequences.for_EventSequenceMetrics;

public class when_recording_constraint_index_lag : Specification
{
    readonly System.Diagnostics.Metrics.Meter _meter = new($"constraint-index-lag-{Guid.NewGuid():N}");
    readonly MeterListener _listener = new();
    readonly List<long> _values = [];
    Exception? _error;

    void Establish()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (ReferenceEquals(instrument.Meter, _meter)) listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<long>((_, value, _, _) => _values.Add(value));
        _listener.Start();
    }

    void Because()
    {
        var scope = new Meter<EventSequence>(_meter).BeginEventSequenceScope("store", "ns");
        _error = Catch.Exception(() => scope.ConstraintIndexLag(7));
    }

    void Destroy()
    {
        _listener.Dispose();
        _meter.Dispose();
    }

    [Fact] void should_not_throw_when_creating_the_instrument() => _error.ShouldBeNull();
    [Fact] void should_record_the_lag_as_a_supported_signed_measurement() => _values.ShouldContainOnly(7L);
}

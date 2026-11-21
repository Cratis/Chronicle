// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Compliance;
using Cratis.Metrics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.ProtectedValues.for_UnrecoverableProtectedValueReporter;

public class when_recording_the_unrecoverable_counter : Specification
{
    Meter _meter;
    MeterListener _listener;
    UnrecoverableProtectedValueReporter _reporter;
    long _measurements;
    int _tagCount;

    void Establish()
    {
        _meter = new("5887-spec", "1.0");
        _listener = new()
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter == _meter && instrument.Name == "chronicle.protected_values.unrecoverable") listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((_, measurement, tags, _) =>
        {
            _measurements += measurement;
            _tagCount += tags.Length;
        });
        _listener.Start();
        var meter = Substitute.For<IMeter<UnrecoverableProtectedValueReporter>>();
        meter.ActualMeter.Returns(_meter);
        _reporter = new(NullLogger<UnrecoverableProtectedValueReporter>.Instance, meter);
    }

    void Because()
    {
        var identity = new UnrecoverableProtectedValueReporter.Identity("store", "namespace", "subject", "value", "Release");
        var failure = new EncryptionKeyUnrecoverable(2, new InvalidOperationException());
        _reporter.Report(identity, failure);
        _reporter.Report(identity, failure);
    }

    void Destroy()
    {
        _listener.Dispose();
        _meter.Dispose();
    }

    [Fact] void should_count_the_report_only_once() => _measurements.ShouldEqual(1L);
    [Fact] void should_not_attach_unbounded_identity_tags() => _tagCount.ShouldEqual(0);
}

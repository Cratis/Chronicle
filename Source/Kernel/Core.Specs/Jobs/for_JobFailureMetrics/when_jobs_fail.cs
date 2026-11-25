// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Jobs.for_JobFailureMetrics;

public class when_jobs_fail : Specification
{
    readonly Dictionary<string, long> _counts = [];
    Meter _meter;
    MeterListener _listener;
    string _unit;
    string _name;

    void Establish()
    {
        _meter = new("6127-job-failures-spec", "1.0");
        _listener = new()
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter != _meter)
                {
                    return;
                }

                _name = instrument.Name;
                _unit = instrument.Unit;
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((_, measurement, tags, _) =>
        {
            foreach (var tag in tags)
            {
                if (tag.Key == WellKnownTelemetryAttributes.JobType)
                {
                    var type = (string)tag.Value!;
                    _counts[type] = _counts.GetValueOrDefault(type) + measurement;
                }
            }
        });
        _listener.Start();
    }

    void Because()
    {
        var metrics = new JobFailureMetrics(_meter);
        metrics.Failed("replay");
        metrics.Failed("replay");
        metrics.Failed("catch_up");
    }

    void Destroy()
    {
        _listener.Dispose();
        _meter.Dispose();
    }

    [Fact] void should_publish_under_the_convention_name() => _name.ShouldEqual(WellKnownMetricNames.JobFailed);
    [Fact] void should_publish_the_unit() => _unit.ShouldEqual(WellKnownMetricNames.JobFailedUnit);
    [Fact] void should_count_per_job_type() => _counts["replay"].ShouldEqual(2L);
    [Fact] void should_count_every_job_type_separately() => _counts["catch_up"].ShouldEqual(1L);
}

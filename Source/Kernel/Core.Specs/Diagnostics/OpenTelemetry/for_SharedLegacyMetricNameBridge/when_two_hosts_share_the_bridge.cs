// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_SharedLegacyMetricNameBridge;

[Collection("Shared legacy metric bridge")]
public class when_two_hosts_share_the_bridge : Specification
{
    const string LegacyName = "chronicle-connected-clients";

    readonly string _marker = Guid.NewGuid().ToString("N");
    readonly List<(string Marker, double Value)> _mirrored = [];
    Meter _firstHost = null!;
    Meter _secondHost = null!;
    MeterListener _listener = null!;
    IDisposable _firstLease = null!;
    IDisposable _secondLease = null!;
    List<(string Marker, double Value)> _afterBothRunning = [];
    List<(string Marker, double Value)> _afterFirstStopped = [];
    List<(string Marker, double Value)> _afterBothStopped = [];

    void Establish()
    {
        _firstHost = new Meter(WellKnown.MeterName);
        _secondHost = new Meter(WellKnown.MeterName);
        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Name == LegacyName) listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<int>((_, value, tags, _) =>
        {
            var marker = tags.ToArray().FirstOrDefault(tag => tag.Key == "marker").Value as string;
            if (marker == _marker)
            {
                lock (_mirrored) _mirrored.Add((marker, value));
            }
        });
        _listener.Start();
    }

    void Because()
    {
        var first = _firstHost.CreateGauge<int>(WellKnownMetricNames.ConnectedClientCount);
        var second = _secondHost.CreateGauge<int>(WellKnownMetricNames.ConnectedClientCount);
        var tag = new KeyValuePair<string, object?>("marker", _marker);

        _firstLease = SharedLegacyMetricNameBridge.Acquire();
        _secondLease = SharedLegacyMetricNameBridge.Acquire();
        first.Record(1, tag);
        second.Record(2, tag);
        _afterBothRunning = [.. _mirrored];

        _firstLease.Dispose();
        _mirrored.Clear();
        second.Record(3, tag);
        _afterFirstStopped = [.. _mirrored];

        _secondLease.Dispose();
        _mirrored.Clear();
        second.Record(4, tag);
        _afterBothStopped = [.. _mirrored];
    }

    void Destroy()
    {
        _firstLease.Dispose();
        _secondLease.Dispose();
        _listener.Dispose();
        _firstHost.Dispose();
        _secondHost.Dispose();
    }

    [Fact] void should_mirror_each_measurement_of_each_host_exactly_once() => _afterBothRunning.Select(_ => _.Value).Order().ShouldEqual([1d, 2d]);
    [Fact] void should_keep_mirroring_when_one_host_stops() => _afterFirstStopped.Select(_ => _.Value).ShouldEqual([3d]);
    [Fact] void should_stop_mirroring_when_the_last_host_stops() => _afterBothStopped.ShouldBeEmpty();
}

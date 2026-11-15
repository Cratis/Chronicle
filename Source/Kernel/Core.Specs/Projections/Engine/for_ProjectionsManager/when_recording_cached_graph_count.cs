// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager;

public class when_recording_cached_graph_count : given.a_manager_with_shared_definitions
{
    readonly List<int> _measurements = [];
    MeterListener _listener;

    void Establish()
    {
        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter == _actualMeter && instrument.Name == WellKnownMetricNames.ProjectionCachedGraphs) listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<int>((_, measurement, _, _) => _measurements.Add(measurement));
        _listener.Start();
    }

    async Task Because()
    {
        await _manager.GetCurrent(_eventStore, _namespace, _firstDefinition.Identifier);
        await _manager.GetCurrent(_eventStore, "another", _firstDefinition.Identifier);
        _manager.Evict(_eventStore, _firstDefinition.Identifier);
    }

    void Destroy() => _listener.Dispose();

    [Fact] void should_report_retained_graphs_and_eviction() => _measurements.ShouldEqual([1, 2, 0]);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Storage.Sql.for_SqlStorageTelemetry;

public class when_waiting_for_a_schema_lock : Specification
{
    readonly MeterListener _listener = new();
    readonly string _provider = Guid.NewGuid().ToString();
    readonly List<double> _values = [];
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    string? _unit;
    string? _outcome;
    int _whileWaiting;

    void Establish()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "cratis.chronicle.sql.schema_lock.wait_duration") listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
        {
            var labels = tags.ToArray().ToDictionary(tag => tag.Key, tag => tag.Value);
            if (!Equals(labels["cratis.chronicle.sql.provider"], _provider)) return;
            _unit = instrument.Unit;
            _outcome = (string?)labels["cratis.chronicle.sql.schema_lock.outcome"];
            _values.Add(value);
        });
        _listener.Start();
    }

    async Task Because()
    {
        var waiting = SqlStorageTelemetry.WaitForSchemaLock(() => _release.Task, _provider);
        _whileWaiting = _values.Count;
        _release.SetResult();
        await waiting;
    }

    void Destroy() => _listener.Dispose();

    [Fact] void should_not_record_before_acquisition_finishes() => _whileWaiting.ShouldEqual(0);
    [Fact] void should_record_one_wait() => _values.Count.ShouldEqual(1);
    [Fact] void should_measure_elapsed_seconds() => _values.Single().ShouldBeGreaterThan(0d);
    [Fact] void should_use_seconds_as_the_unit() => _unit.ShouldEqual("s");
    [Fact] void should_identify_successful_acquisition() => _outcome.ShouldEqual("acquired");
}

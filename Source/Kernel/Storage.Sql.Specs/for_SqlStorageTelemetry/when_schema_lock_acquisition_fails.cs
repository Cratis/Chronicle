// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.Storage.Sql.for_SqlStorageTelemetry;

public class when_schema_lock_acquisition_fails : Specification
{
    readonly MeterListener _listener = new();
    readonly string _provider = Guid.NewGuid().ToString();
    readonly InvalidOperationException _failure = new("not acquired");
    Exception? _error;
    string? _outcome;
    int _measurements;

    void Establish()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "cratis.chronicle.sql.schema_lock.wait_duration") listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<double>((_, _, tags, _) =>
        {
            var labels = tags.ToArray().ToDictionary(tag => tag.Key, tag => tag.Value);
            if (!Equals(labels["cratis.chronicle.sql.provider"], _provider)) return;
            _outcome = (string?)labels["cratis.chronicle.sql.schema_lock.outcome"];
            _measurements++;
        });
        _listener.Start();
    }

    async Task Because() => _error = await Catch.Exception(() => SqlStorageTelemetry.WaitForSchemaLock(() => Task.FromException(_failure), _provider));

    void Destroy() => _listener.Dispose();

    [Fact] void should_preserve_the_acquisition_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_record_one_failed_wait() => _measurements.ShouldEqual(1);
    [Fact] void should_identify_the_failed_acquisition() => _outcome.ShouldEqual("failed");
}

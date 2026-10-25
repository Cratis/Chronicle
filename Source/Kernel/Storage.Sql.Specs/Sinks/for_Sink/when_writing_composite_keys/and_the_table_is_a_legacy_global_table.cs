// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.given;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_writing_composite_keys;

public class and_the_table_is_a_legacy_global_table : a_composite_keyed_sql_read_model
{
    readonly MeterListener _listener = new();
    Exception? _error;
    DynamicReadModelEntity[] _rows;
    string? _scope;
    int _measurements;

    protected override EventStoreNamespaceName Namespace => EventStoreNamespaceName.NotSet;

    void Establish()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "chronicle-sql-composite-key-writes-legacy") listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<int>((_, _, tags, _) =>
        {
            _scope = (string?)tags.ToArray().Single(tag => tag.Key == "scope").Value;
            _measurements++;
        });
        _listener.Start();
    }

    async Task Because()
    {
        var legacy = await ReopenAsLegacy();
        _error = await Catch.Exception(() => legacy.ApplyChanges(CompositeKey("a", "b"), ChangesetNamed("one"), 1UL));
        _rows = await _harness.ReadStoredRows();
    }

    void Destroy() => _listener.Dispose();

    [Fact] void should_keep_writing() => _error.ShouldBeNull();
    [Fact] void should_write_the_legacy_format() => _rows.Single()["id"].ShouldEqual("a_b");
    [Fact] void should_count_the_legacy_write() => _measurements.ShouldEqual(1);
    [Fact] void should_tag_the_write_as_global() => _scope.ShouldEqual("global");
}

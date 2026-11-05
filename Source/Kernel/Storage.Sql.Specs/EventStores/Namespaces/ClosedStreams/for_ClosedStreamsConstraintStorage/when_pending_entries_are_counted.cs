// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage;

[Collection(SqlServerCollection.Name)]
public class when_pending_entries_are_counted(SqlServerFixture fixture) : EventSourceIds.for_spelling_variants.given.a_spelling_storage(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    static readonly string[] _names = ["cratis.chronicle.sql.closed_stream.pending_review_entries", "chronicle-sql-closed-stream-entries-pending-review"];
    readonly List<(string Name, int Value)> _measurements = [];
    MeterListener _listener;
    int _afterFirstRead;

    void Establish()
    {
        _listener = new()
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (_names.Contains(instrument.Name))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        _listener.SetMeasurementEventCallback<int>((instrument, value, _, _) =>
        {
            lock (_measurements)
            {
                _measurements.Add((instrument.Name, value));
            }
        });
        _listener.Start();
    }

    async Task Because()
    {
        await _harness.ClosedStreams.Close(new(new(EventSourceId: "Alice"), "owner", 1, null));
        await _harness.Execute("UPDATE ClosedStreams SET ExactSpelling = 0");
        await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "alice"));
        _afterFirstRead = Count();
        await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "ALICE"));
    }

    void Destroy() => _listener.Dispose();

    [Fact] void should_count_the_entry() => _afterFirstRead.ShouldBeGreaterThan(0);
    [Fact] void should_not_count_it_again_on_a_later_read() => Count().ShouldEqual(_afterFirstRead);

    int Count()
    {
        lock (_measurements)
        {
            return _measurements.Sum(_ => _.Value);
        }
    }
}

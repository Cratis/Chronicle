// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_ExactIdentifiers;

public class when_translating_lookups_for_sql_server() : given.a_storage_that_captures_statements(true)
{
    const string Exact = "COLLATE Latin1_General_100_BIN2";

    string _headBySource;
    string _tailBySource;
    string _tailByStream;
    string _nextBySource;
    string _lastBefore;
    string _lastInstance;

    async Task Because()
    {
        _headBySource = await Capture(() => _events.GetHeadSequenceNumber(eventSourceId: "Order-1 "));
        _tailBySource = await Capture(() => _events.GetTailSequenceNumber(eventSourceId: "Order-1 "));
        _tailByStream = await Capture(() => _events.GetTailSequenceNumber(eventStreamId: "order-1", eventStreamType: "Orders"));
        _nextBySource = await Capture(() => _events.GetNextSequenceNumberGreaterOrEqualThan(EventSequenceNumber.First, eventSourceId: "ＯＲＤＥＲ-1"));
        _lastBefore = await Capture(() => _events.TryGetLastEventBefore(new EventTypeId("order-placed"), "Order-1 ", 10UL));
        _lastInstance = await Capture(() => _events.TryGetLastInstanceOfAny("Order-1 ", [new EventTypeId("order-placed")]));
    }

    async Task<string> Capture(Func<Task> read)
    {
        _captured.Clear();
        try
        {
            await read();
        }
        catch (InvalidOperationException)
        {
            // The captured reader has no rows, so reads that expect one fail after the statement is recorded.
        }

        return string.Join('\n', _captured.All);
    }

    [Fact]
    void should_compare_the_event_source_exactly_when_finding_the_head()
    {
        _headBySource.ShouldContain(SourceSeek);
        _headBySource.ShouldContain(Exact);
        _headBySource.ShouldContain("DATALENGTH(");
    }

    [Fact]
    void should_compare_the_event_source_exactly_when_finding_the_tail()
    {
        _tailBySource.ShouldContain(SourceSeek);
        _tailBySource.ShouldContain(Exact);
        _tailBySource.ShouldContain("DATALENGTH(");
    }

    [Fact]
    void should_compare_the_stream_exactly_when_finding_the_tail()
    {
        _tailByStream.ShouldContain(StreamTypeSeek);
        _tailByStream.ShouldContain(StreamIdSeek);
        _tailByStream.ShouldContain(Exact);
        _tailByStream.ShouldContain("DATALENGTH(");
    }

    [Fact]
    void should_compare_the_event_source_exactly_when_finding_the_next_event()
    {
        _nextBySource.ShouldContain(SourceSeek);
        _nextBySource.ShouldContain(Exact);
    }

    [Fact]
    void should_compare_the_event_source_exactly_when_finding_the_last_event_before()
    {
        _lastBefore.ShouldContain(SourceSeek);
        _lastBefore.ShouldContain(Exact);
    }

    [Fact]
    void should_compare_the_event_source_exactly_when_finding_the_last_instance()
    {
        _lastInstance.ShouldContain(SourceSeek);
        _lastInstance.ShouldContain(Exact);
    }
}

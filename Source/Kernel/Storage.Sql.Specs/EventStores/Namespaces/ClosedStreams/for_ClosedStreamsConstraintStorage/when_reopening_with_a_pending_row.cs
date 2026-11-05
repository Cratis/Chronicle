// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage;

[Collection(SqlServerCollection.Name)]
public class when_reopening_with_a_pending_row(SqlServerFixture fixture) : EventSourceIds.for_spelling_variants.given.a_spelling_storage(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    bool _reopened;
    ClosedStream? _pendingAfter;
    ClosedStream? _exactAfter;
    ClosedStream[] _all;

    async Task Because()
    {
        await _harness.ClosedStreams.Close(new(new(EventSourceId: "Alice"), "owner", 1, null));
        await _harness.Execute("UPDATE ClosedStreams SET ExactSpelling = 0");
        await _harness.ClosedStreams.Close(new(new(EventSourceId: "alice"), "owner", 2, null));
        _reopened = await _harness.ClosedStreams.Reopen("owner", new(EventSourceId: "alice"));
        _pendingAfter = await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "Alice"));
        _exactAfter = await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "alice"));
        _all = [.. await _harness.ClosedStreams.GetForOwner("owner")];
    }

    [Fact] void should_report_the_exact_row_as_reopened() => _reopened.ShouldBeTrue();
    [Fact] void should_remove_only_the_exact_row() => _all.Length.ShouldEqual(1);
    [Fact] void should_keep_the_pending_row() => _pendingAfter!.SequenceNumber.Value.ShouldEqual(1UL);
    [Fact] void should_keep_the_stream_closed_through_the_pending_row() => _exactAfter!.SequenceNumber.Value.ShouldEqual(1UL);
}

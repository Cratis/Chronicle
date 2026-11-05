// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSourceIds.for_spelling_variants.when_closing_streams;

[Collection(SqlServerCollection.Name)]
public class on_sql_server(SqlServerFixture fixture) : given.a_spelling_storage(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    ClosedStream? _otherCaseBefore;
    ClosedStream? _paddedBefore;
    ClosedStream? _first;
    ClosedStream? _otherCase;
    ClosedStream? _padded;
    ClosedStream[] _all;

    async Task Because()
    {
        await _harness.ClosedStreams.Close(new(new(EventSourceId: "Alice"), "owner", 1, null));
        _otherCaseBefore = await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "alice"));
        _paddedBefore = await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "Alice "));
        await _harness.ClosedStreams.Close(new(new(EventSourceId: "alice"), "owner", 2, null));
        await _harness.ClosedStreams.Close(new(new(EventSourceId: "Alice "), "owner", 3, null));
        _first = await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "Alice"));
        _otherCase = await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "alice"));
        _padded = await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "Alice "));
        _all = [.. await _harness.ClosedStreams.GetForOwner("owner")];
    }

    [Fact] void should_not_report_the_lower_case_id_as_closed_before_it_is_closed() => _otherCaseBefore.ShouldBeNull();
    [Fact] void should_not_report_the_id_with_a_trailing_space_as_closed_before_it_is_closed() => _paddedBefore.ShouldBeNull();
    [Fact] void should_keep_the_first_closure_unchanged() => _first!.SequenceNumber.Value.ShouldEqual(1UL);
    [Fact] void should_close_the_lower_case_id_independently() => _otherCase!.SequenceNumber.Value.ShouldEqual(2UL);
    [Fact] void should_close_the_id_with_a_trailing_space_independently() => _padded!.SequenceNumber.Value.ShouldEqual(3UL);
    [Fact] void should_store_one_closure_per_id() => _all.Length.ShouldEqual(3);
}

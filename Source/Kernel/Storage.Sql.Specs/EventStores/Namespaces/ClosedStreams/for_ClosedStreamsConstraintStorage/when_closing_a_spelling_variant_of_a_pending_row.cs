// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage;

[Collection(SqlServerCollection.Name)]
public class when_closing_a_spelling_variant_of_a_pending_row(SqlServerFixture fixture) : EventSourceIds.for_spelling_variants.given.a_spelling_storage(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    ClosedStream[] _all;
    ClosedStream? _pending;
    ClosedStream? _exact;
    ClosedStream? _thirdSpelling;

    async Task Because()
    {
        await _harness.ClosedStreams.Close(new(new(EventSourceId: "Alice"), "owner", 1, null));
        await _harness.Execute("UPDATE ClosedStreams SET ExactSpelling = 0");
        await _harness.ClosedStreams.Close(new(new(EventSourceId: "alice"), "owner", 2, null));
        _pending = await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "Alice"));
        _exact = await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "alice"));
        _thirdSpelling = await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "ALICE"));
        _all = [.. await _harness.ClosedStreams.GetForOwner("owner")];
    }

    [Fact] void should_add_a_new_exact_row() => _all.Length.ShouldEqual(2);
    [Fact] void should_keep_the_pending_row_unchanged() => _pending!.SequenceNumber.Value.ShouldEqual(1UL);
    [Fact] void should_return_the_exact_row_for_its_own_spelling() => _exact!.SequenceNumber.Value.ShouldEqual(2UL);
    [Fact] void should_let_the_pending_row_cover_another_spelling() => _thirdSpelling!.SequenceNumber.Value.ShouldEqual(1UL);
}

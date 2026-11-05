// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage;

[Collection(SqlServerCollection.Name)]
public class when_a_pending_row_covers_another_spelling(SqlServerFixture fixture) : EventSourceIds.for_spelling_variants.given.a_spelling_storage(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    string[] _warnings;

    async Task Because()
    {
        await _harness.ClosedStreams.Close(new(new(EventSourceId: "Alice"), "owner", 1, null));
        await _harness.Execute("UPDATE ClosedStreams SET ExactSpelling = 0");
        await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "alice"));
        await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "ALICE"));
        await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "Alice"));
        _warnings = [.. _harness.Warnings.Messages];
    }

    [Fact] void should_log_one_warning() => _warnings.Length.ShouldEqual(1);
    [Fact] void should_log_the_count() => _warnings[0].ShouldContain("1 closed-stream entries");
    [Fact] void should_not_log_the_identifier() => _warnings[0].ShouldNotContain("Alice");
}

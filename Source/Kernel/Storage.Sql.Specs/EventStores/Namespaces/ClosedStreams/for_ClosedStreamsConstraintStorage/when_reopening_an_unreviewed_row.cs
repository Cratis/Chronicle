// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage;

[Collection(SqlServerCollection.Name)]
public class when_reopening_an_unreviewed_row(SqlServerFixture fixture) : EventSourceIds.for_spelling_variants.given.a_spelling_storage(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    bool _reopened;
    ClosedStream? _sameSpelling;
    ClosedStream? _otherSpelling;
    bool _reopenedAfterReview;
    ClosedStream? _afterReview;

    async Task Because()
    {
        await _harness.ClosedStreams.Close(new(new(EventSourceId: "alice"), "owner", 1, null));
        await _harness.Execute("UPDATE ClosedStreams SET ExactSpelling = 0");
        _reopened = await _harness.ClosedStreams.Reopen("owner", new(EventSourceId: "alice"));
        _sameSpelling = await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "alice"));
        _otherSpelling = await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "Alice"));
        await _harness.Execute("UPDATE ClosedStreams SET ExactSpelling = 1");
        _reopenedAfterReview = await _harness.ClosedStreams.Reopen("owner", new(EventSourceId: "alice"));
        _afterReview = await _harness.ClosedStreams.GetFor("owner", new(EventSourceId: "alice"));
    }

    [Fact] void should_report_nothing_reopened() => _reopened.ShouldBeFalse();
    [Fact] void should_keep_the_stream_closed() => _sameSpelling.ShouldNotBeNull();
    [Fact] void should_keep_the_other_spelling_closed() => _otherSpelling.ShouldNotBeNull();
    [Fact] void should_reopen_after_review() => _reopenedAfterReview.ShouldBeTrue();
    [Fact] void should_leave_the_stream_open_after_review() => _afterReview.ShouldBeNull();
}

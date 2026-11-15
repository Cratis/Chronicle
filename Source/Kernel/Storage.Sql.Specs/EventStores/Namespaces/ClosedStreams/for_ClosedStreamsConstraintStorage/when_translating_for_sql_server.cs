// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_ExactIdentifiers.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage;

public class when_translating_for_sql_server : Specification
{
    static readonly ClosedStreamDimensions[] _masks =
    [
        ClosedStreamDimensions.EventStreamType | ClosedStreamDimensions.EventStreamId,
        ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamType | ClosedStreamDimensions.EventStreamId
    ];

    readonly CapturedStatements _captured = new();
    ClosedStreamsConstraintStorage _storage;
    IReadOnlyList<string> _covering;
    IReadOnlyList<string> _all;

    void Establish()
    {
        var options = new DbContextOptionsBuilder<NamespaceDbContext>()
            .AddConceptAsSupport()
            .WithoutServiceProviderCaching()
            .AddInterceptors(_captured.CreateInterceptors())
            .UseSqlServer("Server=.;Database=x")
            .Options;
        var database = Substitute.For<IDatabase>();
        database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(new NamespaceDbContext(options), () => { })));
        _storage = new("capture-store", "capture-namespace", EventSequenceId.Log, database);
    }

    async Task Because()
    {
        await _storage.GetCovering(new(EventSourceId: "Order-1 ", EventStreamType: "Orders", EventStreamId: "order-1"), _masks);
        _covering = _captured.All;
        _captured.Clear();
        await _storage.GetAll(new(EventSourceId: "Order-1 ", EventStreamId: "order-1"));
        _all = _captured.All;
    }

    [Fact] void should_check_covering_closures_in_one_statement() => _covering.Count.ShouldEqual(1);
    [Fact] void should_seek_by_sequence() => _covering[0].ShouldContain("[EventSequenceId] = @");
    [Fact] void should_seek_by_stream_type() => _covering[0].ShouldContain("[StreamType] = @");
    [Fact] void should_seek_by_stream_id() => _covering[0].ShouldContain("[StreamId] = @");
    [Fact] void should_compare_with_the_column_collation() => _covering[0].ShouldNotContain("COLLATE");
    [Fact] void should_filter_all_closures_by_event_source_id() => _all[0].ShouldContain("[EventSourceId] = @");
    [Fact] void should_filter_all_closures_with_the_column_collation() => _all[0].ShouldNotContain("COLLATE");
}

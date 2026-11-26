// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage;

public class when_ordering_and_paging_decoded_closures : Specification
{
    static readonly string[] _spellings = ["Order-1\u007F", "Order-1 !", "Order-1  ", "Order-1 ", "Order-1"];
    static readonly string[] _expected = ["Order-1", "Order-1 ", "Order-1  ", "Order-1 !", "Order-1\u007F"];

    ClosedStream[] _owners;
    ClosedStream[] _sourceIds;
    ClosedStream[] _sourceTypes;
    ClosedStream[] _streamTypes;
    ClosedStream[] _streamIds;
    ClosedStream[] _page;
    ClosedStream[] _withoutTake;
    ClosedStream[] _emptyPage;
    ClosedStream[] _pastEnd;

    void Because()
    {
        _owners = Order(spelling => new(new(EventStreamId: "stream"), spelling, 1, null));
        _sourceIds = Order(spelling => new(new(EventSourceId: spelling), "owner", 1, null));
        _sourceTypes = Order(spelling => new(new(EventSourceType: spelling), "owner", 1, null));
        _streamTypes = Order(spelling => new(new(EventStreamType: spelling), "owner", 1, null));
        _streamIds = Order(spelling => new(new(EventStreamId: spelling), "owner", 1, null));
        _page = [.. ClosedStreamsConstraintStorage.OrderAndPage(_sourceIds.Reverse(), 1, 2)];
        _withoutTake = [.. ClosedStreamsConstraintStorage.OrderAndPage(_sourceIds.Reverse(), 1, null)];
        _emptyPage = [.. ClosedStreamsConstraintStorage.OrderAndPage(_sourceIds, 0, 0)];
        _pastEnd = [.. ClosedStreamsConstraintStorage.OrderAndPage(_sourceIds, _spellings.Length, 2)];
    }

    static ClosedStream[] Order(Func<string, ClosedStream> closure) => [.. ClosedStreamsConstraintStorage.OrderAndPage(_spellings.Select(closure), 0, null)];

    [Fact] void should_sort_owners_ordinally() => _owners.Select(_ => _.Owner.Value).ShouldEqual(_expected);
    [Fact] void should_sort_event_source_ids_ordinally() => _sourceIds.Select(_ => _.Scope.EventSourceId!.Value).ShouldEqual(_expected);
    [Fact] void should_sort_event_source_types_ordinally() => _sourceTypes.Select(_ => _.Scope.EventSourceType!.Value).ShouldEqual(_expected);
    [Fact] void should_sort_stream_types_ordinally() => _streamTypes.Select(_ => _.Scope.EventStreamType!.Value).ShouldEqual(_expected);
    [Fact] void should_sort_stream_ids_ordinally() => _streamIds.Select(_ => _.Scope.EventStreamId!.Value).ShouldEqual(_expected);
    [Fact] void should_sort_before_skipping_and_taking() => _page.Select(_ => _.Scope.EventSourceId!.Value).ShouldEqual(["Order-1 ", "Order-1  "]);
    [Fact] void should_skip_without_taking() => _withoutTake.Select(_ => _.Scope.EventSourceId!.Value).ShouldEqual(_expected.Skip(1));
    [Fact] void should_return_no_rows_when_taking_zero() => _emptyPage.ShouldBeEmpty();
    [Fact] void should_return_no_rows_when_skipping_past_the_end() => _pastEnd.ShouldBeEmpty();
}

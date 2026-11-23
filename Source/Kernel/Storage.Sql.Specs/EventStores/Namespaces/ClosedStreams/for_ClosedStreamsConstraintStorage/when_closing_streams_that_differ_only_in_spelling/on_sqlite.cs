// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage.when_closing_streams_that_differ_only_in_spelling;

public class on_sqlite() : conformance(Sqlite())
{
    [Fact] void should_list_closures_in_ordinal_order() => _all.Select(_ => _.Scope.EventSourceId!.Value).ShouldEqual(_spellings.Order(StringComparer.Ordinal));
    [Fact] void should_page_in_the_same_order_as_the_full_list() => _page.ShouldEqual(_all.Skip(1).Take(3));
    [Fact] void should_skip_without_limiting_the_remaining_rows() => _withoutTake.ShouldEqual(_all.Skip(1));
    [Fact] void should_filter_by_the_exact_spelling_before_paging() => _filtered.Select(_ => _.Scope.EventStreamId!.Value).ShouldEqual(["Order-1 "]);
}

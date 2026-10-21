// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Chronicle.Sequences.for_ClosedStream.when_listing;

public class and_a_source_filter_is_requested : given.closed_scopes
{
    void Establish() => _queryContexts.Current.Returns(QueryContext.NotSet with { Paging = new(0, 2, true) });

    async Task Because() => _rows = [.. await ClosedStream.ClosedStreams(_storage, _queryContexts, "store", "ns", "event-log", eventSourceId: "two")];

    [Fact] void should_return_only_matching_rows() => _rows.Single().EventSourceId.ShouldEqual("two");
    [Fact] void should_report_the_filtered_total() => _queryContexts.Current.TotalItems.ShouldEqual(1);
}

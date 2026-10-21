// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Chronicle.Sequences.for_ClosedStream.when_listing;

public class and_no_paging_is_requested : given.closed_scopes
{
    void Establish() => _queryContexts.Current.Returns(QueryContext.NotSet with { Paging = Paging.NotPaged });

    async Task Because() => _rows = [.. await ClosedStream.ClosedStreams(_storage, _queryContexts, "store", "ns", "event-log")];

    [Fact] void should_return_all_rows_for_the_client_query() => _rows.Length.ShouldEqual(3);
    [Fact] void should_report_the_total() => _queryContexts.Current.TotalItems.ShouldEqual(3);
}

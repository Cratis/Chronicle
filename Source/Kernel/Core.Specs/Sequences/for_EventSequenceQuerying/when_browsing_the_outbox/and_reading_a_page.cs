// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences.for_EventSequenceQuerying.when_browsing_the_outbox;

public class and_reading_a_page : given.an_outbox_with_a_mixed_catalog
{
    ulong _count;

    async Task Because() => (_, _count) = await EventSequenceQuerying.QueryPage(_storage, _compliance, "store", "default", "outbox", EventSequenceQueryCriteria.Empty, 0, 10, EventSequenceQuerySort.Default);

    [Fact] void should_count_only_public_types() => _sequence.Received(1).GetCountMatching(Arg.Is<EventSequenceQueryCriteria>(_ => OnlyPublic(_)));
    [Fact] void should_page_only_public_types() => _sequence.Received(1).GetPage(Arg.Is<EventSequenceQueryCriteria>(_ => OnlyPublic(_)), 0, 10, Arg.Any<EventSequenceQuerySort>(), Arg.Any<CancellationToken>());
    [Fact] void should_report_the_storage_count() => _count.ShouldEqual(5UL);
}

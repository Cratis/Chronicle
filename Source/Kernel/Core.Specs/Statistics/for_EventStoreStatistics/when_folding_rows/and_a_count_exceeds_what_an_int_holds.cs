// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Statistics.for_EventStoreStatistics.when_folding_rows;

/// <summary>
/// The rows count per event type per namespace, so any one of them fits an int comfortably - but their sum across
/// a busy store need not, and a total that silently wraps into a negative number is worse than no total at all.
/// </summary>
public class and_a_count_exceeds_what_an_int_holds : Specification
{
    EventStoreStatistics _result;

    void Because() => _result = EventStoreStatistics.From(
    [
        new() { EventType = "registered", Namespace = "acme", Count = int.MaxValue },
        new() { EventType = "archived", Namespace = "acme", Count = int.MaxValue }
    ]);

    [Fact] void should_not_overflow_the_total() => _result.TotalEvents.ShouldEqual(int.MaxValue * 2L);
}

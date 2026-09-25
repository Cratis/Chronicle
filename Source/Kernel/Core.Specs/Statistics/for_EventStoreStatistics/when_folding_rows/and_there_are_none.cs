// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Statistics.for_EventStoreStatistics.when_folding_rows;

public class and_there_are_none : Specification
{
    EventStoreStatistics _result;

    void Because() => _result = EventStoreStatistics.From([]);

    [Fact] void should_report_no_events() => _result.TotalEvents.ShouldEqual(0L);
    [Fact] void should_report_no_event_types() => _result.EventTypes.ShouldEqual(0);
    [Fact] void should_report_no_namespaces() => _result.Namespaces.ShouldEqual(0);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_an_administrator_reads_system_statistics : given.an_http_statistics_reader
{
    EventStatistics _result;

    async Task Because()
    {
        BeginRequest();
        _result = await EventStatistics.StatisticsForEventStore(_reader, "System");
    }

    [Fact] void should_aggregate_the_total_across_parallel_namespace_reads() => _result.TotalEvents.ShouldEqual(8);
    [Fact] void should_return_both_namespaces() => _result.Namespaces.ShouldEqual(2);
    [Fact] void should_combine_the_shared_event_type() => _result.EventTypes.ShouldEqual(1);
    [Fact] void should_aggregate_the_event_type_count() => _result.PerEventType.Single().Count.ShouldEqual(8);
    [Fact] async Task should_authorize_each_namespace_against_the_current_roster() => await _privileges.Received(2).IsActiveAdministrator(_administratorId);
}

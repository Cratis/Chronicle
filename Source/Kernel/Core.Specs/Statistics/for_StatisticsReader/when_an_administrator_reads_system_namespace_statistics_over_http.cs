// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_an_administrator_reads_system_namespace_statistics_over_http : given.an_http_statistics_reader
{
    EventStatistics _result;

    async Task Because()
    {
        BeginRequest();
        _result = await EventStatistics.StatisticsForNamespace(_reader, "System", "first");
    }

    [Fact] void should_return_the_total() => _result.TotalEvents.ShouldEqual(3);
    [Fact] void should_return_the_event_type() => _result.PerEventType.Single().EventType.ShouldEqual("shared");
    [Fact] void should_return_one_namespace() => _result.Namespaces.ShouldEqual(1);
    [Fact] async Task should_authorize_against_the_current_roster() => await _privileges.Received().IsActiveAdministrator(_administratorId);
    [Fact] async Task should_not_enumerate_other_namespaces() => await _namespaces.DidNotReceive().GetAll();
}

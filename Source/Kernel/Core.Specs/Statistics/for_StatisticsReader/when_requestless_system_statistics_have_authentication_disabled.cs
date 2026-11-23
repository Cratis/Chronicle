// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Statistics.for_StatisticsReader;

public class when_requestless_system_statistics_have_authentication_disabled : given.an_http_statistics_reader
{
    EventStatistics _result;

    protected override Authentication AuthenticationOptions => new() { Enabled = false };

    protected override bool HasAmbientRequest => false;

    async Task Because()
    {
        BeginRequest();
        _result = await EventStatistics.StatisticsForEventStore(_reader, "System");
    }

    [Fact] void should_return_the_statistics_without_an_identity() => _result.TotalEvents.ShouldEqual(8);
}

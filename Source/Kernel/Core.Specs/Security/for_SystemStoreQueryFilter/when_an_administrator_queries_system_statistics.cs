// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreQueryFilter;

public class when_an_administrator_queries_system_statistics : given.a_query_filter
{
    [Theory]
    [InlineData("Cratis.Chronicle.Statistics.EventStatistics.StatisticsForEventStore")]
    [InlineData("Cratis.Chronicle.Statistics.EventStatistics.StatisticsForNamespace")]
    public async Task should_not_refuse_the_statistics_query(string query)
    {
        var result = await _filter.OnPerform(ContextFor(query, ("eventStore", "System"), ("namespace", "first")));
        result.ShouldBeNull();
    }
}

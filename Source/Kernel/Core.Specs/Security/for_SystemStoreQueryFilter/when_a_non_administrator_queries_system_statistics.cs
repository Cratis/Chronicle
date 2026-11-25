// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_SystemStoreQueryFilter;

public class when_a_non_administrator_queries_system_statistics : given.a_query_filter
{
    [Theory]
    [MemberData(nameof(NonAdministrators))]
    public async Task should_refuse_store_statistics(string caller)
    {
        _caller = CallerNamed(caller);
        var result = await _filter.OnPerform(ContextFor(
            "Cratis.Chronicle.Statistics.EventStatistics.StatisticsForEventStore", ("eventStore", "System")));
        result.IsAuthorized.ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(NonAdministrators))]
    public async Task should_refuse_namespace_statistics(string caller)
    {
        _caller = CallerNamed(caller);
        var result = await _filter.OnPerform(ContextFor(
            "Cratis.Chronicle.Statistics.EventStatistics.StatisticsForNamespace", ("eventStore", "System"), ("namespace", "first")));
        result.IsAuthorized.ShouldBeFalse();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_AlertIncidentQueries.when_querying;

public class and_counts_are_scoped : given.scoped_queries
{
    AlertIncidentSummary _result;

    async Task Because() => _result = await AlertIncidentSummary.GetOpenIncidentCounts("affected", _storage, _readiness, "tenant");

    [Fact] async Task should_scope_counts_in_storage() => await _incidents.Received(1).GetOpenCounts(new("affected", "tenant"));
    [Fact] void should_return_the_readiness_status() => _result.Status.ShouldEqual(AlertIncidentsReadinessState.Ready);
}

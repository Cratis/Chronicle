// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_AlertIncidentQueries.when_querying;

public class and_the_view_is_catchingup : given.scoped_queries
{
    AlertIncidentPage _result;

    void Establish() => _readiness.Get().Returns(AlertIncidentsReadinessState.CatchingUp);
    async Task Because() => _result = await AlertIncidentPage.GetOpenIncidents("affected", _storage, _readiness);

    [Fact] void should_distinguish_non_ready_empty() => _result.Status.ShouldEqual(AlertIncidentsReadinessState.CatchingUp);
}

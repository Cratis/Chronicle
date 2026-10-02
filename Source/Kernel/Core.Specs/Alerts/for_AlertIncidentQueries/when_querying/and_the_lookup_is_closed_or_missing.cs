// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_AlertIncidentQueries.when_querying;

public class and_the_lookup_is_closed_or_missing : given.scoped_queries
{
    AlertIncidentLookup _result;

    async Task Because() => _result = await AlertIncidentLookup.GetOpenIncident("affected", _id, _storage, _readiness, "tenant");

    [Fact] async Task should_scope_the_detail_in_storage() => await _incidents.Received(1).GetOpen(new("affected", "tenant"), _id);
    [Fact] void should_report_normal_not_found() => _result.Incident.ShouldBeNull();
    [Fact] void should_report_ready() => _result.Status.ShouldEqual(AlertIncidentsReadinessState.Ready);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentQueries.when_querying;

public class and_the_page_is_scoped_and_bounded : given.scoped_queries
{
    AlertIncidentPage _result;

    async Task Because() => _result = await AlertIncidentPage.GetOpenIncidents("affected", _storage, _readiness, "tenant", "observer", AlertConditionKind.PartitionFailing, AlertSeverity.Critical, int.MaxValue, 10UL, _id);

    [Fact]
    async Task should_propagate_all_filters() => await _incidents.Received(1).GetOpenPage(
        new(new("affected", "tenant"), "observer", AlertConditionKind.PartitionFailing, AlertSeverity.Critical), new(10UL, _id), 500);
    [Fact] void should_report_ready_empty() => _result.Status.ShouldEqual(AlertIncidentsReadinessState.Ready);
    [Fact] void should_return_empty_items() => _result.Items.ShouldBeEmpty();
}

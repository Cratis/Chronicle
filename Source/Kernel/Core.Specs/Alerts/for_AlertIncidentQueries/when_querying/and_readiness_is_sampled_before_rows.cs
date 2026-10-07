// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentQueries.when_querying;

public class and_readiness_is_sampled_before_rows : given.scoped_queries
{
    readonly List<string> _calls = [];

    void Establish()
    {
        _readiness.Get().Returns(_ =>
        {
            _calls.Add("status");
            return AlertIncidentsReadinessState.Ready;
        });
        _incidents.GetOpenPage(Arg.Any<AlertIncidentFilter>(), Arg.Any<AlertIncidentCursor?>(), Arg.Any<int>()).Returns(_ =>
        {
            _calls.Add("page");
            return new AlertIncidentStoragePage([], null);
        });
        _incidents.GetOpen(Arg.Any<AlertIncidentScope>(), _id).Returns(_ =>
        {
            _calls.Add("lookup");
            return (AlertIncident?)null;
        });
        _incidents.GetOpenCounts(Arg.Any<AlertIncidentScope>()).Returns<IEnumerable<AlertIncidentCount>>(_ =>
        {
            _calls.Add("counts");
            return [];
        });
        _calls.Clear();
    }

    async Task Because()
    {
        await AlertIncidentPage.GetOpenIncidents("affected", _storage, _readiness);
        await AlertIncidentLookup.GetOpenIncident("affected", _id, _storage, _readiness);
        await AlertIncidentSummary.GetOpenIncidentCounts("affected", _storage, _readiness);
    }

    [Fact] void should_sample_health_before_each_storage_read() => _calls.ToArray().ShouldEqual(["status", "page", "status", "lookup", "status", "counts"]);
}

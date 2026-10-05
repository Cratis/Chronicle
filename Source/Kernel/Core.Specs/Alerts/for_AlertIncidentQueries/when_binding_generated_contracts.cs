// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Alerts;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentQueries;

public class when_binding_generated_contracts : Specification
{
    IAlertIncidentsStorage _incidents;
    Contracts.Alerts.IAlerts _service;
    AlertIncident _row;
    Contracts.Queries.QueryResult<Contracts.Alerts.AlertIncidentPageResponse> _page;
    Contracts.Queries.QueryResult<Contracts.Alerts.AlertIncidentLookupResponse> _lookup;
    Contracts.Queries.QueryResult<Contracts.Alerts.AlertIncidentSummaryResponse> _counts;
    Contracts.Alerts.GetOpenIncidentsRequest _request;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        _incidents = Substitute.For<IAlertIncidentsStorage>();
        var readiness = Substitute.For<IAlertIncidentsReadiness>();
        readiness.Get().Returns(AlertIncidentsReadinessState.Ready);
        storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default).AlertIncidents.Returns(_incidents);
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(2)).AddTicks(123);
        _row = new(new IncidentId(Guid.NewGuid()), new("affected", "tenant", "observer", EventSequenceId.Log, "[none]"), "future-condition", AlertSeverity.Critical, new(3, now, now, FailureKind.Handling, "recorded"), now, 10UL, now, 11UL, true, null);
        _incidents.GetOpenPage(Arg.Any<AlertIncidentFilter>(), Arg.Any<AlertIncidentCursor?>(), Arg.Any<int>())
            .Returns(new AlertIncidentStoragePage([_row], new(10UL, _row.Id)));
        _incidents.GetOpen(Arg.Any<AlertIncidentScope>(), _row.Id).Returns(_row);
        _incidents.GetOpenCounts(Arg.Any<AlertIncidentScope>()).Returns([new AlertIncidentCount("tenant", "future-condition", AlertSeverity.Critical, 1)]);
        _service = new Services.Alerts.Alerts(storage, readiness, NullLogger<Services.Alerts.Alerts>.Instance);
        _request = new()
        {
            EventStore = "affected",
            Namespace = "tenant",
            MinimumSeverity = Contracts.Alerts.AlertSeverity.Critical,
            Limit = 100,
            AfterRaisedSequenceNumber = 9,
            AfterIncidentId = _row.Id
        };
    }

    async Task Because()
    {
        _page = Serializer.DeepClone(await _service.GetOpenIncidents(Serializer.DeepClone(_request)));
        _lookup = Serializer.DeepClone(await _service.GetOpenIncident(new() { EventStore = "affected", Namespace = "tenant", IncidentId = _row.Id }));
        _counts = Serializer.DeepClone(await _service.GetOpenIncidentCounts(new() { EventStore = "affected", Namespace = "tenant" }));
    }

    [Fact] void should_return_successful_page() => _page.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_recorded_evidence() => _page.Data.Items.Single().Evidence.Message.ShouldEqual("recorded");
    [Fact] void should_preserve_lossless_timestamps() => ((DateTimeOffset)_page.Data.Items.Single().RaisedAt).ShouldEqual(_row.RaisedAt!.Value);
    [Fact] void should_preserve_continuation() => _page.Data.Next.IncidentId.ShouldEqual(_row.Id.Value);
    [Fact] void should_preserve_readiness() => _page.Data.Status.ShouldEqual(Contracts.Alerts.AlertIncidentsReadinessState.Ready);
    [Fact] void should_bind_lookup() => _lookup.Data.Incident.Id.ShouldEqual(_row.Id.Value);
    [Fact] void should_bind_counts() => _counts.Data.Counts.Single().Count.ShouldEqual(1L);
    [Fact] async Task should_preserve_optional_filters_and_paired_cursor() => await _incidents.Received(1).GetOpenPage(new(new("affected", "tenant"), null, null, AlertSeverity.Critical), new(9UL, _row.Id), 100);
}

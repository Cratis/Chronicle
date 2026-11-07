// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.Alerts.for_Alerts;

public class when_a_non_administrator_requests_incidents : for_StoreCapabilityGuard.given.a_store_capability_caller
{
    IStorage _storage;
    IAlertIncidentsReadiness _readiness;
    Alerts _alerts;

    void Establish()
    {
        Decide(AuthorizationReason.NotGranted, false);
        _storage = Substitute.For<IStorage>();
        _readiness = Substitute.For<IAlertIncidentsReadiness>();
        _alerts = new(_storage, _readiness, NullLogger<Alerts>.Instance);
    }

    async Task Because()
    {
        await _alerts.GetOpenIncident(new() { EventStore = "shop", Namespace = "tenant", IncidentId = Guid.NewGuid() }, _context);
        await _alerts.GetOpenIncidents(new() { EventStore = "shop", Namespace = "tenant" }, _context);
        await _alerts.GetOpenIncidentCounts(new() { EventStore = "shop" }, _context);
    }

    [Fact] void should_decide_the_alert_capability_for_every_query() => _asked.Select(_ => _.Capability).Distinct().Single().ShouldEqual(WellKnownCapabilities.ReadAlerts);
    [Fact] void should_decide_at_the_requested_store_only() => _asked.Select(_ => _.Scope.EventStore!.Value).Distinct().Single().ShouldEqual("shop");
    [Fact] void should_decide_once_per_query() => _asked.Count.ShouldEqual(3);
}

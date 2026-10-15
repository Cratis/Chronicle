// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsMetricsOwnerKeeper.when_pinging;

public class and_incident_metrics_are_disabled : given.a_keeper
{
    void Establish()
    {
        _grainFactory.ClearReceivedCalls();
        _keeper = new(_grainFactory, Substitute.For<ILogger<AlertIncidentsMetricsOwnerKeeper>>(), options: Options.Create(new ChronicleOptions { Alerts = new() { IncidentMetricsEnabled = false } }));
    }

    Task Because() => _keeper.Ping();

    [Fact] void should_not_resolve_the_owner() => _grainFactory.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_ping_the_owner() => _owner.DidNotReceive().Ensure();
}

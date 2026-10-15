// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsMetricsOwner.when_refreshing;

public class and_incident_metrics_are_disabled : given.an_owner
{
    void Establish()
    {
        _storage.ClearReceivedCalls();
        _owner = new(_storage, _readiness, _gauge, Substitute.For<ILogger<AlertIncidentsMetricsOwner>>(), options: Options.Create(new ChronicleOptions { Alerts = new() { IncidentMetricsEnabled = false } }));
    }

    Task Because() => _owner.RefreshAsync();

    [Fact] void should_not_check_readiness() => _readiness.DidNotReceive().Get();
    [Fact] void should_not_access_storage() => _storage.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_read_incident_counts() => _incidents.DidNotReceive().GetOpenCountsByObserver(Arg.Any<CancellationToken>());
    [Fact] void should_not_publish_a_snapshot() => _gauge.DidNotReceive().Publish(Arg.Any<object>(), Arg.Any<AlertIncidentsGaugeSnapshot>());
}

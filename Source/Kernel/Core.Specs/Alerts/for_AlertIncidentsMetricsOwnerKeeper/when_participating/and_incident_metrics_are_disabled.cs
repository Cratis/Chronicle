// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsMetricsOwnerKeeper.when_participating;

public class and_incident_metrics_are_disabled : given.a_keeper
{
    ISiloLifecycle _lifecycle;

    void Establish()
    {
        _lifecycle = Substitute.For<ISiloLifecycle>();
        _keeper = new(_grainFactory, Substitute.For<ILogger<AlertIncidentsMetricsOwnerKeeper>>(), options: Options.Create(new ChronicleOptions { Alerts = new() { IncidentMetricsEnabled = false } }));
    }

    void Because() => _keeper.Participate(_lifecycle);

    [Fact] void should_not_subscribe_a_background_loop() => _lifecycle.ReceivedCalls().ShouldBeEmpty();
}

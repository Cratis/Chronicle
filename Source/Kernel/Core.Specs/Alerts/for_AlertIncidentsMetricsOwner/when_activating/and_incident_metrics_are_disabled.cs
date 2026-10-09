// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsMetricsOwner.when_activating;

public class and_incident_metrics_are_disabled : given.an_owner
{
    Exception _exception;

    void Establish() => _owner = new(_storage, _readiness, _gauge, Substitute.For<ILogger<AlertIncidentsMetricsOwner>>(), options: Options.Create(new ChronicleOptions { Alerts = new() { IncidentMetricsEnabled = false } }));

    async Task Because() => _exception = await Catch.Exception(() => _owner.OnActivateAsync(CancellationToken.None));

    [Fact] void should_not_activate_the_gauge() => _gauge.DidNotReceive().Activate(Arg.Any<object>());
    [Fact] void should_not_attempt_to_register_a_grain_timer_without_a_runtime() => _exception.ShouldBeNull();
}

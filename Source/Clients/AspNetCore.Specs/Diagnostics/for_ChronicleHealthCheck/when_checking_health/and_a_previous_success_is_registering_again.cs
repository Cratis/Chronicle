// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.AspNetCore.Diagnostics.for_ChronicleHealthCheck.when_checking_health;

public class and_a_previous_success_is_registering_again : given.a_health_check
{
    HealthCheckResult _result;

    void Establish()
    {
        _lifecycle.IsConnected.Returns(true);
        _eventStore.Registration.Returns(new RegistrationOutcome(true, []) { IsRegistering = true });
    }

    async Task Because() => _result = await _healthCheck.CheckHealthAsync(_context);

    [Fact] void should_report_unhealthy() => _result.Status.ShouldEqual(HealthStatus.Unhealthy);
    [Fact] void should_report_the_specified_in_progress_message() => _result.Description.ShouldEqual("Registration is in progress for event store 'the-store'.");
}

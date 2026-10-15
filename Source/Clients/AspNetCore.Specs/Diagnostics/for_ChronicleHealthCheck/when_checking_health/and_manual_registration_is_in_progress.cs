// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.AspNetCore.Diagnostics.for_ChronicleHealthCheck.when_checking_health;

public class and_manual_registration_is_in_progress : given.a_health_check
{
    HealthCheckResult _result;

    void Establish()
    {
        _lifecycle.IsConnected.Returns(true);
        _options.AutoDiscoverAndRegister = false;
        _eventStore.Registration.Returns(RegistrationOutcome.NotRun with { IsRegistering = true });
        _context.Registration = new HealthCheckRegistration(ChronicleHealthCheck.Name, Substitute.For<IHealthCheck>(), HealthStatus.Degraded, null);
    }

    async Task Because() => _result = await _healthCheck.CheckHealthAsync(_context);

    [Fact] void should_use_the_configured_failure_status() => _result.Status.ShouldEqual(HealthStatus.Degraded);
    [Fact] void should_report_the_specified_in_progress_message() => _result.Description.ShouldEqual("Registration is in progress for event store 'the-store'.");
}

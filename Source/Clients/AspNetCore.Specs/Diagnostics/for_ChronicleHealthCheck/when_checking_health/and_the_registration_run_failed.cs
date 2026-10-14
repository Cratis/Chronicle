// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.AspNetCore.Diagnostics.for_ChronicleHealthCheck.when_checking_health;

public class and_the_registration_run_failed : given.a_health_check
{
    readonly Exception _failure = new("Registration stopped.");
    HealthCheckResult _result;

    void Establish()
    {
        _lifecycle.IsConnected.Returns(true);
        _eventStore.Registration.Returns(new RegistrationOutcome(true, ImmutableList<ArtifactRegistration>.Empty, _failure));
    }

    async Task Because() => _result = await _healthCheck.CheckHealthAsync(_context);

    [Fact] void should_report_unhealthy() => _result.Status.ShouldEqual(HealthStatus.Unhealthy);
    [Fact] void should_report_registration_failed() => _result.Description.ShouldContain("Registration failed");
    [Fact] void should_include_the_run_failure() => _result.Exception.ShouldEqual(_failure);
    [Fact] void should_have_no_failed_artifacts() => ((IEnumerable<string>)_result.Data["failedArtifacts"]).ShouldBeEmpty();
}

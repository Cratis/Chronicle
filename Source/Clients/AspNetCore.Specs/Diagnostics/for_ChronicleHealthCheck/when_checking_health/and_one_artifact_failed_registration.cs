// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.AspNetCore.Diagnostics.for_ChronicleHealthCheck.when_checking_health;

public class and_one_artifact_failed_registration : given.a_health_check
{
    HealthCheckResult _result;

    void Establish()
    {
        _lifecycle.IsConnected.Returns(true);
        _eventStore.Registration.Returns(new RegistrationOutcome(true, ImmutableList.Create(
            new ArtifactRegistration(typeof(string), new Exception("Artifact could not be built.")),
            new ArtifactRegistration(typeof(int), null))));
    }

    async Task Because() => _result = await _healthCheck.CheckHealthAsync(_context);

    [Fact] void should_report_unhealthy() => _result.Status.ShouldEqual(HealthStatus.Unhealthy);
    [Fact] void should_report_registration_failed() => _result.Description.ShouldContain("Registration failed");
    [Fact] void should_only_include_the_failed_artifact_type() => ((IEnumerable<string>)_result.Data["failedArtifacts"]).ShouldContainOnly(typeof(string).FullName);
    [Fact] void should_not_report_a_run_failure() => _result.Exception.ShouldBeNull();
}

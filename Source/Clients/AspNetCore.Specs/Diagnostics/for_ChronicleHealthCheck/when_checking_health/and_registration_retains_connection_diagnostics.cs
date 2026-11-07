// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.AspNetCore.Diagnostics.for_ChronicleHealthCheck.when_checking_health;

public class and_registration_retains_connection_diagnostics : given.a_health_check
{
    Exception _failure;
    HealthCheckResult _result;

    void Establish()
    {
        _failure = new AggregateException("Connection setup failed.",
            new InvalidCertificateOrPassword("synthetic-health-certificate-marker.pfx"),
            new ConnectionUnavailable("chronicle://synthetic-health-user-marker:1753198871?apiKey=synthetic-health-tail-marker@server:35000"));
        _lifecycle.IsConnected.Returns(true);
        _eventStore.Registration.Returns(new RegistrationOutcome(true, ImmutableList<ArtifactRegistration>.Empty, _failure));
    }

    async Task Because() => _result = await _healthCheck.CheckHealthAsync(_context);

    [Fact] void should_report_unhealthy() => _result.Status.ShouldEqual(HealthStatus.Unhealthy);
    [Fact] void should_retain_the_actual_aggregate_exception() => ReferenceEquals(_result.Exception, _failure).ShouldBeTrue();
    [Fact] void should_not_expose_markers_in_the_outer_description() => given.diagnostic_payloads.ShouldNotExposeMarkers([_result.Description ?? string.Empty]);
    [Fact] void should_not_expose_markers_in_the_retained_exception_graph() => given.diagnostic_payloads.ShouldNotExposeMarkers(given.diagnostic_payloads.Text(_result.Exception));
    [Fact] void should_not_expose_markers_in_health_data() => given.diagnostic_payloads.ShouldNotExposeMarkers(given.diagnostic_payloads.Text(_result.Data));
}

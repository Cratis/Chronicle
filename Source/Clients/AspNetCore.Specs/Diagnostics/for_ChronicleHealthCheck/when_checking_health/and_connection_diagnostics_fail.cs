// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cratis.Chronicle.AspNetCore.Diagnostics.for_ChronicleHealthCheck.when_checking_health;

public class and_connection_diagnostics_fail : given.a_health_check
{
    Dictionary<Exception, HealthCheckResult> _results;
    Exception[] _failures;
    Exception _currentFailure;

    void Establish()
    {
        const string raw = "chronicle://synthetic-health-user-marker:1753198871?certificatePath=synthetic-health-tail-marker@server:35000";
        _failures =
        [
            new ConnectionUnavailable(raw),
            new ConnectionUnavailable(new ChronicleConnectionString(raw)),
            new InvalidServerAddress("synthetic-health-user-marker:1753198871"),
            new CertificateDoesNotExist("synthetic-health-certificate-marker.pfx"),
            new InvalidCertificateOrPassword("synthetic-health-certificate-marker.pfx"),
            new UnknownLoadBalancerStrategy("synthetic-health-setting-marker"),
            new NoSrvRecordsFound("synthetic-health-user-marker")
        ];
        _results = [];
        _currentFailure = _failures[0];
        _client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns<IEventStore>(_ => throw _currentFailure);
    }

    async Task Because()
    {
        foreach (var failure in _failures.Append(new AggregateException("Connection setup failed.", _failures)))
        {
            _currentFailure = failure;
            _results.Add(failure, await _healthCheck.CheckHealthAsync(_context));
        }
    }

    [Fact] void should_report_each_failure_as_unhealthy() => _results.Values.All(result => result.Status == HealthStatus.Unhealthy).ShouldBeTrue();
    [Fact] void should_retain_each_actual_exception_without_replacing_it() => _results.All(pair => ReferenceEquals(pair.Key, pair.Value.Exception)).ShouldBeTrue();
    [Fact] void should_not_expose_markers_in_descriptions() => given.diagnostic_payloads.ShouldNotExposeMarkers(_results.Values.Select(result => result.Description ?? string.Empty));
    [Fact] void should_not_expose_markers_in_retained_exception_objects() => given.diagnostic_payloads.ShouldNotExposeMarkers(_results.Values.SelectMany(result => given.diagnostic_payloads.Text(result.Exception)));
    [Fact] void should_not_expose_markers_in_health_data() => given.diagnostic_payloads.ShouldNotExposeMarkers(_results.Values.SelectMany(result => given.diagnostic_payloads.Text(result.Data)));
}

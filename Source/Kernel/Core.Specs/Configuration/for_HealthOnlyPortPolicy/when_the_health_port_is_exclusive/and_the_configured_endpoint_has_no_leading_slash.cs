// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_HealthOnlyPortPolicy.when_the_health_port_is_exclusive;

public class and_the_configured_endpoint_has_no_leading_slash : Specification
{
    const int HealthPort = 8080;
    ChronicleOptions _options;
    bool _healthPath;
    bool _otherPath;

    void Establish() => _options = new ChronicleOptions
    {
        Port = 35000,
        HealthCheckEndpoint = "healthz",
        Health = new Health { Port = HealthPort, Exclusive = true }
    };

    void Because()
    {
        _healthPath = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/healthz");
        _otherPath = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/api/event-store");
    }

    [Fact] void should_serve_the_health_path() => _healthPath.ShouldBeFalse();
    [Fact] void should_reject_another_path() => _otherPath.ShouldBeTrue();
}

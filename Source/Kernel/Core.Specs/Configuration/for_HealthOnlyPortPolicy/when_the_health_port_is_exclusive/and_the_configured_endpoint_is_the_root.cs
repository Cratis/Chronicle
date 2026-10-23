// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_HealthOnlyPortPolicy.when_the_health_port_is_exclusive;

public class and_the_configured_endpoint_is_the_root : Specification
{
    const int HealthPort = 8080;
    ChronicleOptions _options;
    bool _rootRejected;
    bool _liveRejected;
    bool _readyRejected;
    bool _otherRejected;

    void Establish() => _options = new ChronicleOptions
    {
        Port = 35000,
        HealthCheckEndpoint = "/",
        Health = new Health { Port = HealthPort, Exclusive = true }
    };

    void Because()
    {
        _rootRejected = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/");
        _liveRejected = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/live");
        _readyRejected = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/ready");
        _otherRejected = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/api/event-store");
    }

    [Fact] void should_serve_the_root() => _rootRejected.ShouldBeFalse();
    [Fact] void should_serve_liveness_below_the_root() => _liveRejected.ShouldBeFalse();
    [Fact] void should_serve_readiness_below_the_root() => _readyRejected.ShouldBeFalse();
    [Fact] void should_reject_another_path() => _otherRejected.ShouldBeTrue();
}

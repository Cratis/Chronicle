// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_HealthOnlyPortPolicy.when_the_health_port_is_exclusive;

public class and_health_subpaths_are_requested : given.an_exclusive_dedicated_health_port
{
    bool _liveRejected;
    bool _readyRejected;
    bool _unknownRejected;
    bool _paddedRejected;

    void Because()
    {
        _liveRejected = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/HEALTH/live/");
        _readyRejected = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/health/ready");
        _unknownRejected = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/health/other");
        _paddedRejected = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/health/live ");
    }

    [Fact] void should_admit_liveness() => _liveRejected.ShouldBeFalse();
    [Fact] void should_admit_readiness() => _readyRejected.ShouldBeFalse();
    [Fact] void should_reject_unknown_subpaths() => _unknownRejected.ShouldBeTrue();
    [Fact] void should_reject_whitespace_padded_subpaths() => _paddedRejected.ShouldBeTrue();
}

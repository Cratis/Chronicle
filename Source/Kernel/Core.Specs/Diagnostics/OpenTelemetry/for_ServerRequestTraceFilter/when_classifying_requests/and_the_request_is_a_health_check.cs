// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_ServerRequestTraceFilter.when_classifying_requests;

public class and_the_request_is_a_health_check : Specification
{
    bool _health;
    bool _live;
    bool _ready;

    void Because()
    {
        _health = ServerRequestTraceFilter.ShouldTrace("/health", "/health");
        _live = ServerRequestTraceFilter.ShouldTrace("/health", "/health/live");
        _ready = ServerRequestTraceFilter.ShouldTrace("/health", "/health/ready");
    }

    [Fact] void should_not_trace_health() => _health.ShouldBeFalse();
    [Fact] void should_not_trace_liveness() => _live.ShouldBeFalse();
    [Fact] void should_not_trace_readiness() => _ready.ShouldBeFalse();
}

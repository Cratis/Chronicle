// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_ServerRequestTraceFilter.when_classifying_requests;

public class and_no_health_check_endpoint_is_configured : Specification
{
    bool _health;
    bool _stream;

    void Because()
    {
        _health = ServerRequestTraceFilter.ShouldTrace(null, "/health");
        _stream = ServerRequestTraceFilter.ShouldTrace(null, "/Cratis.Chronicle.Contracts.Clients.ConnectionService/Connect");
    }

    [Fact] void should_trace_health() => _health.ShouldBeTrue();
    [Fact] void should_not_trace_streams() => _stream.ShouldBeFalse();
}

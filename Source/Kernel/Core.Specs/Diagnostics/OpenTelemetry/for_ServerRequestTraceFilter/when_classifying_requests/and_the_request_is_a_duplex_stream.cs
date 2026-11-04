// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_ServerRequestTraceFilter.when_classifying_requests;

public class and_the_request_is_a_duplex_stream : Specification
{
    bool _result;

    void Because() => _result = ServerRequestTraceFilter.ShouldTrace("/health", "/Cratis.Chronicle.Contracts.Observation.Reactors.Reactors/Observe");

    [Fact] void should_not_trace_the_request() => _result.ShouldBeFalse();
}

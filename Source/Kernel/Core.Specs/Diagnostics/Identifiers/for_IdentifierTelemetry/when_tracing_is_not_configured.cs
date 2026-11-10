// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.Identifiers.for_IdentifierTelemetry;

public class when_tracing_is_not_configured : Specification
{
    IdentifierTelemetry _telemetry;
    string? _result;

    void Establish() => _telemetry = new(new());

    void Because() => _result = _telemetry.ForSpan("customer-42");

    [Fact] void should_hash_by_default() => _telemetry.Tracing.ShouldEqual(EventSourceIdTracing.Hashed);
    [Fact] void should_not_expose_the_raw_id() => _result.ShouldNotEqual("customer-42");
    [Fact] void should_hash_consistently_within_the_process() => new IdentifierTelemetry(new()).ForSpan("customer-42").ShouldEqual(_result);
}

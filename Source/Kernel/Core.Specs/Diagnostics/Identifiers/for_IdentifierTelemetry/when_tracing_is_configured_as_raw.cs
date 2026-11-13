// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Diagnostics.Identifiers.for_IdentifierTelemetry;

public class when_tracing_is_configured_as_raw : Specification
{
    string? _result;

    void Because() => _result = new IdentifierTelemetry(new Telemetry { EventSourceIdTracing = EventSourceIdTracing.Raw }).ForSpan("customer-42");

    [Fact] void should_keep_the_raw_id() => _result.ShouldEqual("customer-42");
}

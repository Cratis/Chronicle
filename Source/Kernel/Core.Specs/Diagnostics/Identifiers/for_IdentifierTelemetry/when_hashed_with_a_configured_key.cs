// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Diagnostics.Identifiers.for_IdentifierTelemetry;

public class when_hashed_with_a_configured_key : Specification
{
    const string Key = "chronicle-identifier-hash-key-for-specs-0001";
    string? _result;

    void Because() => _result = new IdentifierTelemetry(new Telemetry
    {
        EventSourceIdTracing = EventSourceIdTracing.Hashed,
        EventSourceIdHashKey = Key
    }).ForSpan("customer-42");

    [Fact] void should_use_the_configured_key() => _result.ShouldEqual(new IdentifierHasher(Key).Hash("customer-42"));
}

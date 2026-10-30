// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Configuration.for_Telemetry;

public class when_serializing : Specification
{
    const string HashKey = "01234567890123456789012345678901";
    string _json;

    void Because() => _json = JsonSerializer.Serialize(new Telemetry
    {
        EventSourceIdTracing = EventSourceIdTracing.Hashed,
        EventSourceIdHashKey = HashKey
    });

    [Fact] void should_not_include_the_hash_key_property() => _json.ShouldNotContain(nameof(Telemetry.EventSourceIdHashKey));
    [Fact] void should_not_include_the_hash_key_value() => _json.ShouldNotContain(HashKey);
    [Fact] void should_include_the_tracing_mode() => _json.ShouldContain(nameof(Telemetry.EventSourceIdTracing));
}

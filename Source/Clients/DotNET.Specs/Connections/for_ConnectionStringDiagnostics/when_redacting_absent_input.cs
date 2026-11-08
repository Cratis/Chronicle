// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics;

public class when_redacting_absent_input : Specification
{
    string[] _diagnostics;

    void Because() => _diagnostics =
    [
        ChronicleConnectionString.Redact(null!),
        ChronicleConnectionString.Redact(string.Empty),
        ChronicleConnectionString.Redact("   ")
    ];

    [Fact] void should_not_invent_a_default_endpoint_for_diagnostics() => _diagnostics.ShouldEachConformTo(value => value == "REDACTED");
}

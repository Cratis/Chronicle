// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics.given;

namespace Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics;

public class when_redacting_safe_endpoints : Specification
{
    string[] _diagnostics;

    void Because()
    {
        var input = $"chronicle://{a_connection_string_corpus.Username}:{a_connection_string_corpus.Password}@server:35000";
        var connection = new ChronicleConnectionString(input);
        var builder = new ChronicleConnectionStringBuilder(input);
        _diagnostics =
        [
            ChronicleConnectionString.Redact(input),
            connection.Redacted,
            builder.BuildRedacted(),
            diagnostic_surfaces.DebuggerText(connection),
            diagnostic_surfaces.DebuggerText(builder)
        ];
    }

    [Fact] void should_preserve_the_demonstrably_safe_endpoint() => _diagnostics.ShouldEachConformTo(value => value == "chronicle://REDACTED:REDACTED@server:35000");
    [Fact] void should_remove_credentials_from_all_diagnostic_surfaces() => diagnostic_surfaces.ShouldContainNoMarkers(_diagnostics);
}

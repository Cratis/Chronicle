// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics.given;

namespace Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics;

public class when_mutating_and_cloning_uncertain_settings : Specification
{
    string[] _diagnostics;

    void Because()
    {
        var builder = new ChronicleConnectionStringBuilder("chronicle://server:35000/synthetic-path-marker") { Host = "other" };
        var programmatic = new ChronicleConnectionStringBuilder { Host = "server", Port = 35000 };
        programmatic["neutral"] = "synthetic-query-value-marker";
        programmatic.Remove("neutral");
        var connection = new ChronicleConnectionString("chronicle://server:35000/synthetic-path-marker");
        var cloned = connection.WithCredentials(a_connection_string_corpus.Username, a_connection_string_corpus.Password);
        var reattributed = new ChronicleConnectionString($"chronicle://server:35000?Host={a_connection_string_corpus.Username}&Port=35000");
        _diagnostics =
        [
            builder.BuildRedacted(),
            programmatic.BuildRedacted(),
            cloned.Redacted,
            connection.WithApiKey(a_connection_string_corpus.ApiKey).Redacted,
            reattributed.WithCredentials(a_connection_string_corpus.Username, a_connection_string_corpus.Password).Redacted,
            diagnostic_surfaces.DebuggerText(cloned)
        ];
    }

    [Fact] void should_keep_uncertain_provenance_opaque() => _diagnostics.ShouldEachConformTo(value => value == "REDACTED");
    [Fact] void should_not_expose_markers_after_mutation_or_cloning() => diagnostic_surfaces.ShouldContainNoMarkers(_diagnostics);
}

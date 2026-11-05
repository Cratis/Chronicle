// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics.given;

namespace Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics;

public class when_redacting_rejected_and_numeric_inputs : Specification
{
    List<string> _diagnostics;
    List<string> _exceptions;
    string _directNumericMessage;

    void Because()
    {
        string[] inputs =
        [
            $"chronicle://[{a_connection_string_corpus.Username}:{a_connection_string_corpus.NumericPassword}",
            $"chronicle://server:{a_connection_string_corpus.Password}",
            $"chronicle://{a_connection_string_corpus.Username}:nonnumeric?neutral={a_connection_string_corpus.Tail}",
            $"synthetic://{a_connection_string_corpus.Username}:{a_connection_string_corpus.Password}@server:35000"
        ];
        _diagnostics = [];
        _exceptions = [];
        _directNumericMessage = new InvalidServerAddress($"{a_connection_string_corpus.Username}:{a_connection_string_corpus.NumericPassword}").Message;
        _exceptions.AddRange(diagnostic_surfaces.ExceptionText(new InvalidServerAddress(a_connection_string_corpus.NumericPassword)));
        _exceptions.AddRange(diagnostic_surfaces.ExceptionText(new InvalidServerAddress($"{a_connection_string_corpus.Username}:{a_connection_string_corpus.NumericPassword}")));

        foreach (var input in inputs)
        {
            var exception = Catch.Exception(() => _ = new ChronicleConnectionString(input));
            exception.ShouldNotBeNull();
            _exceptions.AddRange(diagnostic_surfaces.ExceptionText(exception));
            _exceptions.AddRange(diagnostic_surfaces.ExceptionText(new ConnectionUnavailable(input)));
            _diagnostics.Add(ChronicleConnectionString.Redact(input));
        }
    }

    [Fact] void should_not_echo_an_unproven_numeric_port() => _directNumericMessage.ShouldEqual("The server address 'REDACTED' is not a valid host with an optional port");
    [Fact] void should_use_an_opaque_static_fallback() => _diagnostics.ShouldEachConformTo(value => value == "REDACTED");
    [Fact] void should_not_expose_complete_or_partial_markers_in_exception_surfaces() => diagnostic_surfaces.ShouldContainNoMarkers(_exceptions);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics.given;

namespace Cratis.Chronicle.Connections.for_ConnectionStringDiagnostics;

public class when_redacting_uncertain_connection_details : Specification
{
    List<string> _static;
    List<string> _instance;
    List<string> _builder;
    List<string> _debugger;
    List<string> _repeated;
    List<string> _exceptions;

    void Because()
    {
        _static = [];
        _instance = [];
        _builder = [];
        _debugger = [];
        _repeated = [];
        _exceptions = [];

        foreach (var input in a_connection_string_corpus.UncertainInputs)
        {
            var connection = new ChronicleConnectionString(input);
            var builder = new ChronicleConnectionStringBuilder(input);
            _static.Add(ChronicleConnectionString.Redact(input));
            _instance.Add(connection.Redacted);
            _builder.Add(builder.BuildRedacted());
            _debugger.Add(diagnostic_surfaces.DebuggerText(connection));
            _debugger.Add(diagnostic_surfaces.DebuggerText(builder));
            _repeated.Add(connection.Redacted);
            _repeated.Add(builder.BuildRedacted());
            _repeated.Add(ChronicleConnectionString.Redact(ChronicleConnectionString.Redact(input)));
            _exceptions.AddRange(diagnostic_surfaces.ExceptionText(new ConnectionUnavailable(input)));
            _exceptions.AddRange(diagnostic_surfaces.ExceptionText(new ConnectionUnavailable(connection)));
        }
    }

    [Fact] void should_use_opaque_static_diagnostics() => _static.ShouldEachConformTo(value => value == "REDACTED");
    [Fact] void should_use_opaque_instance_diagnostics() => _instance.ShouldEachConformTo(value => value == "REDACTED");
    [Fact] void should_use_opaque_builder_diagnostics() => _builder.ShouldEachConformTo(value => value == "REDACTED");
    [Fact] void should_use_opaque_actual_debugger_members() => _debugger.ShouldEachConformTo(value => value == "REDACTED");
    [Fact] void should_remain_opaque_on_repeated_redaction() => _repeated.ShouldEachConformTo(value => value == "REDACTED");
    [Fact] void should_not_expose_complete_or_partial_markers() => diagnostic_surfaces.ShouldContainNoMarkers(_static.Concat(_instance).Concat(_builder).Concat(_debugger));
    [Fact] void should_not_expose_markers_in_either_unavailable_constructor() => diagnostic_surfaces.ShouldContainNoMarkers(_exceptions);
}

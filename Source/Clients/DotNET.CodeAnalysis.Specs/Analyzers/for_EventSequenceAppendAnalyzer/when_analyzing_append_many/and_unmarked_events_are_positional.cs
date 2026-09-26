// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventSequenceAppendAnalyzer.when_analyzing_append_many;

public class and_unmarked_events_are_positional : given.an_event_sequence_append_analyzer
{
    const string Usage = """
    public class MissingEvent { }

    public class Usage
    {
        public void Append()
        {
            var sequence = new Cratis.Chronicle.EventSequences.EventSequence();
            var events = new System.Collections.Generic.List<MissingEvent> { new MissingEvent() };
            sequence.AppendMany("source", {|#0:events|});
            sequence.AppendMany("source", {|#1:new[] { new MissingEvent() }|});
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.EventSequenceAppendAnalyzer>.VerifyAnalyzer(CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.EventTypeMustHaveAttributeWhenAppended, DiagnosticSeverity.Error, "MissingEvent"),
        new ExpectedDiagnostic(DiagnosticIds.EventTypeMustHaveAttributeWhenAppended, DiagnosticSeverity.Error, "MissingEvent"));

    [Fact] Task should_report_event_type_attribute_diagnostics() => _result;
}

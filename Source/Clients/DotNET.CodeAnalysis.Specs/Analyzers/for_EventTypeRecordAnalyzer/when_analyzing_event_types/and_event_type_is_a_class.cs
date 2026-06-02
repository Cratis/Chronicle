// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventTypeRecordAnalyzer.when_analyzing_event_types;

public class and_event_type_is_a_class : given.an_event_type_record_analyzer
{
    const string Usage = """
    [Cratis.Chronicle.Concepts.Events.EventType]
    {|#0:public class UserRegistered
    {
        public string UserId { get; set; }
    }|}
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.EventTypeRecordAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.EventTypeShouldBeRecordType, DiagnosticSeverity.Warning, "UserRegistered"));

    [Fact] Task should_report_record_type_diagnostic() => _result;
}

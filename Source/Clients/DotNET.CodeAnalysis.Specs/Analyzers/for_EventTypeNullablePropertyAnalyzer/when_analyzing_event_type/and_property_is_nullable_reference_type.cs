// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventTypeNullablePropertyAnalyzer.when_analyzing_event_type;

public class and_property_is_nullable_reference_type : given.an_event_type_nullable_property_analyzer
{
    const string Usage = """
    [EventType]
    public class MyEvent
    {
        public string Name { get; init; } = string.Empty;
        public string? {|#0:Comment|} { get; init; }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.EventTypeNullablePropertyAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.EventTypeHasNullableProperty, DiagnosticSeverity.Warning, "MyEvent", "Comment"));

    [Fact] Task should_report_nullable_property_diagnostic() => _result;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventTypeNullablePropertiesAnalyzer.when_analyzing_event_types;

public class and_event_type_has_nullable_properties : given.an_event_type_nullable_properties_analyzer
{
    const string Usage = """
    [EventType]
    public record MissionAccepted(
        string Name,
        string? {|#0:Comment|},
        DateOnly? {|#1:StartDate|});
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.EventTypeNullablePropertiesAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.EventTypeShouldAvoidNullableProperties, DiagnosticSeverity.Warning, "MissionAccepted", "Comment"),
        new ExpectedDiagnostic(DiagnosticIds.EventTypeShouldAvoidNullableProperties, DiagnosticSeverity.Warning, "MissionAccepted", "StartDate"));

    [Fact] Task should_report_warning_for_each_nullable_property() => _result;
}

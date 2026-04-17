// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventTypeNullablePropertiesAnalyzer.when_analyzing_event_types;

public class and_event_type_has_no_nullable_properties : given.an_event_type_nullable_properties_analyzer
{
    const string Usage = """
    [EventType]
    public record MissionAccepted(
        string Name,
        DateOnly StartDate);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.EventTypeNullablePropertiesAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_diagnostics() => _result;
}

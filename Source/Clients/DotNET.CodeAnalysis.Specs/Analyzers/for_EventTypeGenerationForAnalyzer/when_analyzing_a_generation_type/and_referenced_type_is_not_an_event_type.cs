// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventTypeGenerationForAnalyzer.when_analyzing_a_generation_type;

public class and_referenced_type_is_not_an_event_type : given.an_event_type_generation_for_analyzer
{
    const string Usage = """
    public record CustomerRegisteredV2(string FirstName, string LastName);

    [EventTypeGenerationFor<CustomerRegisteredV2>(1)]
    public record {|#0:CustomerRegisteredV1|}(string Name);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.EventTypeGenerationForAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.EventTypeGenerationForMustReferenceEventType, DiagnosticSeverity.Error, "CustomerRegisteredV1", "CustomerRegisteredV2"));

    [Fact] Task should_report_the_diagnostic() => _result;
}

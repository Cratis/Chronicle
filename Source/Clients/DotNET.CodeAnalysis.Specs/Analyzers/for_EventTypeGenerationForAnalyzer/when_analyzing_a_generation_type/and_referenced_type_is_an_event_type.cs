// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventTypeGenerationForAnalyzer.when_analyzing_a_generation_type;

public class and_referenced_type_is_an_event_type : given.an_event_type_generation_for_analyzer
{
    const string Usage = """
    [EventType("customer-registered", generation: 2)]
    public record CustomerRegisteredV2(string FirstName, string LastName);

    [EventTypeGenerationFor<CustomerRegisteredV2>(1)]
    public record CustomerRegisteredV1(string Name);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.EventTypeGenerationForAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}

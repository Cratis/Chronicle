// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventTypeRecordAnalyzer.when_analyzing_event_types;

public class and_event_type_is_a_record : given.an_event_type_record_analyzer
{
    const string Usage = """
    [Cratis.Chronicle.Concepts.Events.EventType]
    public record UserRegistered(string UserId);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.EventTypeRecordAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostics() => _result;
}

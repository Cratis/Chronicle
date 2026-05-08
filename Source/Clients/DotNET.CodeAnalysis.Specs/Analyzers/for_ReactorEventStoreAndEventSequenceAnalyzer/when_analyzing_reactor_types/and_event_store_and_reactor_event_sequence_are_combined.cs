// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorEventStoreAndEventSequenceAnalyzer.when_analyzing_reactor_types;

public class and_event_store_and_reactor_event_sequence_are_combined : given.a_reactor_event_store_and_event_sequence_analyzer
{
    const string Usage = """
    {|#0:[EventStore("external-store")]
    [Reactor(eventSequence: "custom-sequence")]
    public class InvalidReactor : IReactor
    {
    }|}
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorEventStoreAndEventSequenceAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ReactorCannotCombineEventStoreWithExplicitEventSequence, DiagnosticSeverity.Error, "InvalidReactor", "external-store"));

    [Fact] Task should_report_event_store_and_explicit_event_sequence_conflict() => _result;
}
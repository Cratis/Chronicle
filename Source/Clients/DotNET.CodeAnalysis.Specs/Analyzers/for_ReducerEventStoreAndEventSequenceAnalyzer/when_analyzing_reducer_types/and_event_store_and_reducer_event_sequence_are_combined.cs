// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReducerEventStoreAndEventSequenceAnalyzer.when_analyzing_reducer_types;

public class and_event_store_and_reducer_event_sequence_are_combined : given.a_reducer_event_store_and_event_sequence_analyzer
{
    const string Usage = """
    {|#0:[EventStore("external-store")]
    [Reducer(eventSequence: "custom-sequence")]
    public class InvalidReducer : IReducer
    {
    }|}
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerEventStoreAndEventSequenceAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ReducerCannotCombineEventStoreWithExplicitEventSequence, DiagnosticSeverity.Error, "InvalidReducer", "external-store"));

    [Fact] Task should_report_event_store_and_explicit_event_sequence_conflict() => _result;
}

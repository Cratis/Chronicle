// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_SelfReducerIntentAnalyzer.when_analyzing;

public class and_a_historical_generation_fold_name_is_wrong : given.read_model_sources
{
    Task _result;
    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerIntentAnalyzer>.VerifyAnalyzer(ModelSource("""
        [EventType] public record Changed;
        [EventTypeGenerationFor<Changed>(1)] public record ChangedV1;
        [ReadModel] public record State
        {
            public static State Reducer(ChangedV1 e, State? current) => new();
        }
        """),
        new ExpectedDiagnostic(DiagnosticIds.SelfReducerMethodWillNotBeDiscovered, DiagnosticSeverity.Warning, "Reducer", "its name is not 'Reduce'"));
    [Fact] Task should_recognize_the_historical_event_declaration() => _result;
}

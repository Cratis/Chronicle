// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_SelfReducerIntentAnalyzer.when_analyzing;

public class and_an_inherited_marker_fold_name_is_wrong : given.read_model_sources
{
    Task _result;
    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerIntentAnalyzer>.VerifyAnalyzer(ModelSource("""
        [EventType] public record Changed;
        public record DerivedChange : Changed;
        [ReadModel] public record State
        {
            public static State Reducer(DerivedChange e, State? current) => new();
        }
        """),
        new ExpectedDiagnostic(DiagnosticIds.SelfReducerMethodWillNotBeDiscovered, DiagnosticSeverity.Warning, "Reducer", "its name is not 'Reduce'"));
    [Fact] Task should_recognize_the_inherited_event_declaration() => _result;
}

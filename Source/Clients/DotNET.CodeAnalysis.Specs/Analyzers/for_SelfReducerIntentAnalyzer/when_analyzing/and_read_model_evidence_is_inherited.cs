// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_SelfReducerIntentAnalyzer.when_analyzing;

public class and_read_model_evidence_is_inherited : given.read_model_sources
{
    Task _result;
    void Because() => _result = Verify();
    [Fact] Task should_report_the_declared_fold_once_using_inherited_metadata() => _result;

    static Task Verify() => AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerIntentAnalyzer>.VerifyAnalyzer(ModelSource("""
        [ReadModel] public record Base;
        public record State : Base
        {
            public static State Reducer(Folded e, State? current) => new();
        }
        public record Derived : State;
        """),
        new ExpectedDiagnostic(DiagnosticIds.SelfReducerMethodWillNotBeDiscovered, DiagnosticSeverity.Warning, "Reducer"));
}

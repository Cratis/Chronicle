// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_SelfReducerIntentAnalyzer.when_analyzing;

public class and_the_fold_is_an_instance_method : given.read_model_sources
{
    Task _result;
    void Because() => _result = Verify();
    [Fact] Task should_warn_with_the_instance_reason_and_expected_signature() => _result;

    static async Task Verify()
    {
        await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerIntentAnalyzer>.VerifyAnalyzer(
            ModelSource("[ReadModel] public record State { public State Reduce(Folded e, State? current) => new(); }"),
            new ExpectedDiagnostic(DiagnosticIds.SelfReducerMethodWillNotBeDiscovered, DiagnosticSeverity.Warning, "Reduce", "it is not static", "public static Reduce(event, nullable current[, EventContext])"));
        await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerIntentAnalyzer>.VerifyAnalyzer(
            ModelSource("[ReadModel] public record State { public Task<State> Reducer(Folded e, State? current, EventContext context) => Task.FromResult(new State()); }"),
            new ExpectedDiagnostic(DiagnosticIds.SelfReducerMethodWillNotBeDiscovered, DiagnosticSeverity.Warning, "Reducer", "its name is not 'Reduce' and it is not static"));
    }
}

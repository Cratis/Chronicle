// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_SelfReducerIntentAnalyzer.when_analyzing;

public class and_the_fold_name_is_wrong : given.read_model_sources
{
    Task _result;
    void Because() => _result = Verify();
    [Fact] Task should_warn_with_the_name_reason_and_expected_signature() => _result;

    static async Task Verify()
    {
        foreach (var name in new[] { "Reducer", "reduce", "ReduceOn", "Apply" })
        {
            await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerIntentAnalyzer>.VerifyAnalyzer(
                ModelSource($"[ReadModel] public record State {{ public static State {{|#0:{name}|}}(Folded e, State? current) => new(); }}"),
                new ExpectedDiagnostic(DiagnosticIds.SelfReducerMethodWillNotBeDiscovered, DiagnosticSeverity.Warning, name, "its name is not 'Reduce'", "public static Reduce(event, nullable current[, EventContext])", "returning the model or Task of the model"));
        }
        await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerIntentAnalyzer>.VerifyAnalyzer(
            ModelSource("[ReadModel] public record State { public static Task<State?> Reducer(Folded e, State? current, EventContext context) => Task.FromResult(current); }"),
            new ExpectedDiagnostic(DiagnosticIds.SelfReducerMethodWillNotBeDiscovered, DiagnosticSeverity.Warning, "Reducer"));
        await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerIntentAnalyzer>.VerifyAnalyzer(
            ModelSource("[ReadModel] public record State { public static State Reducer(Folded e, State current) => current; }"),
            new ExpectedDiagnostic(DiagnosticIds.SelfReducerMethodWillNotBeDiscovered, DiagnosticSeverity.Warning, "Reducer"));
    }
}

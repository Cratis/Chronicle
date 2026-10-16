// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_SelfReducerIntentAnalyzer.when_analyzing;

public class and_the_model_is_structurally_recognized : given.read_model_sources
{
    Task _result;
    void Because() => _result = Verify();
    [Fact] Task should_use_a_sibling_reduce_declaration_as_read_model_evidence() => _result;

    static async Task Verify()
    {
        foreach (var current in new[] { "State?", "State" })
        {
            await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerIntentAnalyzer>.VerifyAnalyzer(ModelSource($$"""
                public record State
                {
                    public static State Reduce(Happened e, {{current}} current) => new();
                    public static State Reducer(Folded e, State? current) => new();
                }
                """),
                new ExpectedDiagnostic(DiagnosticIds.SelfReducerMethodWillNotBeDiscovered, DiagnosticSeverity.Warning, "Reducer"));
        }
        await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerIntentAnalyzer>.VerifyAnalyzer(ModelSource("""
            public record State
            {
                public static State Reduce(Happened e, State current) => new();
                public State Reduce(Folded e, State? current) => new();
            }
            """),
            new ExpectedDiagnostic(DiagnosticIds.SelfReducerMethodWillNotBeDiscovered, DiagnosticSeverity.Warning, "it is not static"));
    }
}

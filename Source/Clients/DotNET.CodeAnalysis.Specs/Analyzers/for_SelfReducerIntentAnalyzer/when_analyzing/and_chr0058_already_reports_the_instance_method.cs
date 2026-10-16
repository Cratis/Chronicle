// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_SelfReducerIntentAnalyzer.when_analyzing;

public class and_chr0058_already_reports_the_instance_method : given.read_model_sources
{
    Task _result;
    void Because() => _result = Verify();
    [Fact] Task should_not_duplicate_the_existing_signature_warning() => _result;

    static async Task Verify()
    {
        var source = ModelSource("""
            public record State
            {
                public static State Reduce(Happened e, State? current) => new();
                public State Reduce(Folded e, State? current) => new();
            }
            """);
        await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerIntentAnalyzer>.VerifyAnalyzer(source);
        await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerMethodAnalyzer>.VerifyAnalyzer(source,
            new ExpectedDiagnostic(DiagnosticIds.SelfReducerMethodSignatureMustMatchAllowed, DiagnosticSeverity.Warning, "Reduce"));
    }
}

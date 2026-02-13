// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReducerMethodAnalyzer.when_analyzing_reducer_methods;

public class and_signature_has_invalid_context_parameter : given.a_reducer_method_analyzer
{
    const string Usage = """
    [EventType]
    public class KnownEvent
    {
    }

    public class Reducer : Cratis.Chronicle.Reducers.IReducer
    {
        public void {|#0:On|}(KnownEvent @event, string context)
        {
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerMethodAnalyzer>.VerifyAnalyzer(CreateSource(Usage), new ExpectedDiagnostic(DiagnosticIds.ReducerMethodSignatureMustMatchAllowed, DiagnosticSeverity.Warning, "On"));

    [Fact] Task should_report_invalid_signature_diagnostic() => _result;
}

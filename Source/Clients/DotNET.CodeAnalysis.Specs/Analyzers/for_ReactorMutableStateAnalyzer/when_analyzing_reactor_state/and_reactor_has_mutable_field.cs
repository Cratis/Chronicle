// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorMutableStateAnalyzer.when_analyzing_reactor_state;

public class and_reactor_has_mutable_field : given.a_reactor_mutable_state_analyzer
{
    const string Usage = """
    public class Reactor : Cratis.Chronicle.Reactors.IReactor
    {
        {|#0:int _count|};
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorMutableStateAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ReactorMustNotHaveMutableState, DiagnosticSeverity.Warning, "Reactor", "_count"));

    [Fact] Task should_report_diagnostic() => _result;
}

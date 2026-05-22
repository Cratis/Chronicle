// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorInjectionAnalyzer.when_analyzing_reactor_types;

public class and_reactor_injects_event_log : given.a_reactor_injection_analyzer
{
    const string Usage = """
    public class MyReactor : Cratis.Chronicle.Reactors.IReactor
    {
        public MyReactor({|#0:Cratis.Chronicle.EventSequences.IEventLog eventLog|}) { }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorInjectionAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ReactorMustNotInjectIEventLog, DiagnosticSeverity.Error, "MyReactor"));

    [Fact] Task should_report_event_log_injection_diagnostic() => _result;
}

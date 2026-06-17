// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorMethodAnalyzer.when_analyzing_reactor_methods;

public class and_return_type_is_an_unsupported_side_effect : given.a_reactor_method_analyzer
{
    const string Usage = """
    [EventType]
    public class KnownEvent
    {
    }

    public class NotAnEvent
    {
    }

    public class Reactor : Cratis.Chronicle.Reactors.IReactor
    {
        public NotAnEvent {|#0:On|}(KnownEvent @event)
        {
            return new NotAnEvent();
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorMethodAnalyzer>.VerifyAnalyzer(CreateSource(Usage), new ExpectedDiagnostic(DiagnosticIds.ReactorMethodSignatureMustMatchAllowed, DiagnosticSeverity.Warning, "On"));

    [Fact] Task should_report_invalid_signature_diagnostic() => _result;
}

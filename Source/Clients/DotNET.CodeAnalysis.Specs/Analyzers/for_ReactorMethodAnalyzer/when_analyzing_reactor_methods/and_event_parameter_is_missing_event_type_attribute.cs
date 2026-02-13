// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorMethodAnalyzer.when_analyzing_reactor_methods;

public class and_event_parameter_is_missing_event_type_attribute : given.a_reactor_method_analyzer
{
    const string Usage = """
    public class MissingEvent
    {
    }

    public class Reactor : Cratis.Chronicle.Reactors.IReactor
    {
        public void On({|#0:MissingEvent @event|})
        {
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorMethodAnalyzer>.VerifyAnalyzer(CreateSource(Usage), new ExpectedDiagnostic(DiagnosticIds.ReactorEventParameterMustHaveAttribute, DiagnosticSeverity.Error, "MissingEvent", "On"));

    [Fact] Task should_report_event_type_attribute_diagnostic() => _result;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorMethodAnalyzer.when_analyzing_reactor_methods;

/// <summary>
/// A method the author has marked as a handler, whose first parameter is not an event type: Chronicle will never
/// dispatch to it, and the marker says that was not the intent.
/// </summary>
/// <remarks>
/// The marker is what makes this reportable. Chronicle discovers a handler by its first parameter carrying
/// <c>[EventType]</c>, so without one there is nothing to separate "meant to be a handler, forgot the attribute"
/// from an ordinary helper - and a reactor is mostly ordinary helpers.
/// </remarks>
public class and_event_parameter_is_missing_event_type_attribute : given.a_reactor_method_analyzer
{
    const string Usage = """
    public class MissingEvent
    {
    }

    public class Reactor : Cratis.Chronicle.Reactors.IReactor
    {
        [Cratis.Chronicle.Reactors.OnceOnly]
        public void On({|#0:MissingEvent @event|})
        {
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorMethodAnalyzer>.VerifyAnalyzer(CreateSource(Usage), new ExpectedDiagnostic(DiagnosticIds.ReactorEventParameterMustHaveAttribute, DiagnosticSeverity.Error, "MissingEvent", "On"));

    [Fact] Task should_report_event_type_attribute_diagnostic() => _result;
}

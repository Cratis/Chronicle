// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.CodeFixes.for_AddEventTypeAttributeCodeFixProvider.when_applying_code_fix;

public class and_event_parameter_is_missing_event_type_attribute : given.an_add_event_type_attribute_code_fix_provider
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

    const string FixedUsage = """
    [EventType]
    public class MissingEvent
    {
    }

    public class Reactor : Cratis.Chronicle.Reactors.IReactor
    {
        public void On(MissingEvent @event)
        {
        }
    }
    """;

    Task _result;

    void Because() => _result = CodeFixVerifier<CodeAnalysis.Analyzers.ReactorMethodAnalyzer, CodeAnalysis.CodeFixes.AddEventTypeAttributeCodeFixProvider>.VerifyCodeFix(CreateReactorSource(Usage), CreateReactorSource(FixedUsage).Replace("using System.Threading.Tasks;", "using System.Threading.Tasks;\nusing Cratis.Chronicle.Concepts.Events;"), new ExpectedDiagnostic(DiagnosticIds.ReactorEventParameterMustHaveAttribute, DiagnosticSeverity.Error, "MissingEvent", "On"));

    [Fact] Task should_add_event_type_attribute_to_reactor_event_type() => _result;
}

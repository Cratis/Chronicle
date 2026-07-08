// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorCommandPipelineExecuteOnceOnlyAnalyzer.when_analyzing_reactor_methods;

public class and_method_invokes_execute_without_once_only : given.a_reactor_command_pipeline_execute_analyzer
{
    const string Usage = """
    [Cratis.Chronicle.Concepts.Events.EventType]
    public class SomeEvent
    {
    }

    public class Reactor : Cratis.Chronicle.Reactors.IReactor
    {
        readonly Cratis.Chronicle.Commands.ICommandPipeline _pipeline;

        public Reactor(Cratis.Chronicle.Commands.ICommandPipeline pipeline)
        {
            _pipeline = pipeline;
        }

        public void On(SomeEvent @event)
        {
            {|#0:_pipeline.Execute(new object())|};
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorCommandPipelineExecuteOnceOnlyAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ReactorCommandPipelineExecuteMustBeOnceOnly, DiagnosticSeverity.Warning, "On"));

    [Fact] Task should_report_diagnostic() => _result;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorOnceOnlyAnalyzer.when_analyzing_reactor_methods;

public class and_method_returns_task : given.a_reactor_once_only_analyzer
{
    const string Usage = """
    [Cratis.Chronicle.Concepts.Events.EventType]
    public class SomeEvent
    {
    }

    public class Reactor : Cratis.Chronicle.Reactors.IReactor
    {
        public Task On(SomeEvent @event)
        {
            return Task.CompletedTask;
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorOnceOnlyAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_diagnostic() => _result;
}

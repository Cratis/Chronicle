// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorOnceOnlyAnalyzer.when_analyzing_reactor_methods;

public class and_method_returns_single_event_with_once_only : given.a_reactor_once_only_analyzer
{
    const string Usage = """
    [Cratis.Chronicle.Concepts.Events.EventType]
    public class SomeEvent
    {
    }

    [Cratis.Chronicle.Concepts.Events.EventType]
    public class ResultEvent
    {
    }

    public class Reactor : Cratis.Chronicle.Reactors.IReactor
    {
        [Cratis.Chronicle.Reactors.OnceOnly]
        public ResultEvent On(SomeEvent @event)
        {
            return new ResultEvent();
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorOnceOnlyAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_diagnostic() => _result;
}

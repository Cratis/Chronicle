// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorMethodAnalyzer.when_analyzing_reactor_methods;

public class and_return_type_is_task_of_an_unmarked_side_effect : given.a_reactor_method_analyzer
{
    const string Usage = """
    [EventType]
    public class KnownEvent
    {
    }

    public class CustomSideEffect
    {
    }

    public class Reactor : Cratis.Chronicle.Reactors.IReactor
    {
        public Task<CustomSideEffect> On(KnownEvent @event) => Task.FromResult(new CustomSideEffect());
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorMethodAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}

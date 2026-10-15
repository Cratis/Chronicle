// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReducerMethodAnalyzer.when_analyzing_reducer_methods;

public class and_method_is_dispatchable : given.a_reducer_method_analyzer
{
    [Theory]
    [InlineData("Model", "")]
    [InlineData("Model", ", Cratis.Chronicle.Events.EventContext context")]
    [InlineData("Task<Model>", "")]
    [InlineData("Task<Model>", ", Cratis.Chronicle.Events.EventContext context")]
    [InlineData("Task", "")]
    [InlineData("Task", ", Cratis.Chronicle.Events.EventContext context")]
    [InlineData("Model?", "")]
    [InlineData("Task<Model?>", "")]
    public Task should_not_report_a_diagnostic(string returnType, string contextParameter) =>
        AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerMethodAnalyzer>.VerifyAnalyzer(CreateSource($$"""
        #nullable enable
        [EventType] public class KnownEvent {}
        public class Model {}
        public class Reducer : Cratis.Chronicle.Reducers.IReducerFor<Model>
        {
            public {{returnType}} Reduce(KnownEvent @event, Model? current{{contextParameter}}) => throw new NotImplementedException();
            public Task BeginReplayPartition(string partition) => Task.CompletedTask;
        }
        """));
}

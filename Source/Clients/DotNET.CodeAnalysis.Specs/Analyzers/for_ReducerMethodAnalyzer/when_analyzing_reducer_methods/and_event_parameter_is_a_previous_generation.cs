// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReducerMethodAnalyzer.when_analyzing_reducer_methods;

public class and_event_parameter_is_a_previous_generation : given.a_reducer_method_analyzer
{
    [Theory]
    [InlineData("PreviousEvent")]
    [InlineData("IPreviousEvent")]
    [InlineData("PreviousEventBase")]
    public Task should_accept_the_previous_generation(string eventParameter) =>
        AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerMethodAnalyzer>.VerifyAnalyzer(CreateSource($$"""
        #nullable enable
        public interface IPreviousEvent {}
        public class PreviousEventBase {}
        [EventType] public class CurrentEvent {}
        [Cratis.Chronicle.Events.EventTypeGenerationFor<CurrentEvent>(1)]
        public class PreviousEvent : PreviousEventBase, IPreviousEvent {}
        public class Model {}
        public class Reducer : Cratis.Chronicle.Reducers.IReducerFor<Model>
        {
            public Model Reduce({{eventParameter}} @event, Model? current) => current ?? new Model();
        }
        """));
}

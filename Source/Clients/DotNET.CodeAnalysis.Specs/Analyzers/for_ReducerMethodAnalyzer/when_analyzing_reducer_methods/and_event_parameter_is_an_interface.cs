// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReducerMethodAnalyzer.when_analyzing_reducer_methods;

public class and_event_parameter_is_an_interface : given.a_reducer_method_analyzer
{
    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerMethodAnalyzer>.VerifyAnalyzer(CreateSource("""
    public interface IKnownEvent {}
    [EventType] public class KnownEvent : IKnownEvent {}
    public class Model {}
    public class Reducer : Cratis.Chronicle.Reducers.IReducerFor<Model>
    {
        public Model Reduce(IKnownEvent @event, Model current) => current;
    }
    """));

    [Fact] Task should_accept_an_interface_with_known_event_types() => _result;
}

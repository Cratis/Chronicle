// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReducerMethodAnalyzer.when_analyzing_reducer_methods;

public class and_method_is_a_helper : given.a_reducer_method_analyzer
{
    [Theory]
    [InlineData("private Model Create(string id)")]
    [InlineData("private Model Apply(SomeDto dto, Model? current)")]
    [InlineData("private bool IsClosed(KnownEvent @event)")]
    [InlineData("public static Model Create(string id)")]
    [InlineData("public static Model Ensure(Model? current)")]
    [InlineData("public static Model Apply(SomeDto dto, Model? current)")]
    [InlineData("public static Model Reduce(KnownEvent @event, Model? current)")]
    [InlineData("protected Model Create(string id)")]
    [InlineData("protected Model Apply(SomeDto dto, Model? current)")]
    [InlineData("protected bool IsClosed(Model? current)")]
    public Task should_not_report_a_diagnostic(string signature) =>
        AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerMethodAnalyzer>.VerifyAnalyzer(CreateSource($$"""
        #nullable enable
        [EventType] public class KnownEvent {}
        public class SomeDto {}
        public class Model {}
        public class Reducer : Cratis.Chronicle.Reducers.IReducerFor<Model>
        {
            {{signature}} => throw new NotImplementedException();
        }
        """));
}

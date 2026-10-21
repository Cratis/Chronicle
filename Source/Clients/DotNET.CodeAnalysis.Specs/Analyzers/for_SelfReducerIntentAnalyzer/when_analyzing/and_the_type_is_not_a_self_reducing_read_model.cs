// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_SelfReducerIntentAnalyzer.when_analyzing;

public class and_the_type_is_not_a_self_reducing_read_model : given.read_model_sources
{
    Task _result;
    void Because() => _result = Verify();
    [Fact] Task should_not_infer_read_model_intent_from_a_fold_shape_alone() => _result;

    static Task Verify() => AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerIntentAnalyzer>.VerifyAnalyzer(ModelSource("""
        public record Helper
        {
            public static Helper Reducer(Folded e, Helper? current) => new();
            public Helper Reduce(Folded e, Helper? current) => new();
        }
        [Cratis.Arc.Queries.ModelBound.ReadModel] public record Classic : IReducerFor<Classic>
        {
            public Classic Reduce(Folded e, Classic? current) => new();
            public static Classic Reducer(Folded e, Classic? current) => new();
        }
        [Cratis.Arc.Queries.ModelBound.ReadModel] public abstract record Abstract
        {
            public static Abstract Reducer(Folded e, Abstract? current) => current!;
        }
        [Cratis.Arc.Queries.ModelBound.ReadModel] public record Generic<T>
        {
            public static Generic<T> Reducer(Folded e, Generic<T>? current) => new();
        }
        public class ReadModelAttribute : Attribute;
        [Sample.ReadModel] public record UnrelatedAttribute
        {
            public static UnrelatedAttribute Reducer(Folded e, UnrelatedAttribute? current) => new();
        }
        """));
}

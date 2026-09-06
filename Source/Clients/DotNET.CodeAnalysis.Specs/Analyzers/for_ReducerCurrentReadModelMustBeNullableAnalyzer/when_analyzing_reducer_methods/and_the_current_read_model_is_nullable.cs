// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReducerCurrentReadModelMustBeNullableAnalyzer.when_analyzing_reducer_methods;

public class and_the_current_read_model_is_nullable : given.a_reducer_current_read_model_must_be_nullable_analyzer
{
    const string Usage = """
    [EventType]
    public class SomethingHappened { }

    public class TheReadModel { }

    public class TheReducer : Cratis.Chronicle.Reducers.IReducerFor<TheReadModel>
    {
        public TheReadModel Reduce(SomethingHappened @event, TheReadModel? current) => current!;
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerCurrentReadModelMustBeNullableAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_anything() => _result;
}

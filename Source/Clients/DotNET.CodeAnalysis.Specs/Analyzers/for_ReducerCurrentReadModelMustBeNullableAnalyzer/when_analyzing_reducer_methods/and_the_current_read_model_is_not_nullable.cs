// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReducerCurrentReadModelMustBeNullableAnalyzer.when_analyzing_reducer_methods;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/3947 — a handler promising a non-null current read
/// model was silently left out of dispatch, so its events were never applied.
/// </summary>
public class and_the_current_read_model_is_not_nullable : given.a_reducer_current_read_model_must_be_nullable_analyzer
{
    const string Usage = """
    [EventType]
    public class SomethingHappened { }

    public class TheReadModel { }

    public class TheReducer : Cratis.Chronicle.Reducers.IReducerFor<TheReadModel>
    {
        public TheReadModel Reduce(SomethingHappened @event, {|#0:TheReadModel current|}) => current;
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerCurrentReadModelMustBeNullableAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ReducerCurrentReadModelMustBeNullable, DiagnosticSeverity.Warning, "Reduce", "TheReadModel"));

    [Fact] Task should_report_that_the_current_read_model_must_be_nullable() => _result;
}

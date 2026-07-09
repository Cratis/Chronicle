// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReducerMustNotHaveMutableStateAnalyzer.when_analyzing_reducer;

public class and_reducer_is_stateless : given.a_reducer_must_not_have_mutable_state_analyzer
{
    const string Usage = """
    public interface IDependency
    {
    }

    public class Balance
    {
    }

    public class BalanceReducer(IDependency dependency) : Cratis.Chronicle.Reducers.IReducerFor<Balance>
    {
        readonly IDependency _dependency = dependency;

        public string Name { get; init; }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerMustNotHaveMutableStateAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_diagnostic() => _result;
}

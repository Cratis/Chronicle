// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReducerMustNotHaveMutableStateAnalyzer.when_analyzing_reducer;

public class and_reducer_injects_mongo_collection : given.a_reducer_must_not_have_mutable_state_analyzer
{
    const string Usage = """
    public class Balance
    {
    }

    public class BalanceReducer : Cratis.Chronicle.Reducers.IReducerFor<Balance>
    {
        public BalanceReducer({|#0:MongoDB.Driver.IMongoCollection<Balance> collection|})
        {
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerMustNotHaveMutableStateAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ReducerMustNotHaveMutableState, DiagnosticSeverity.Warning, "BalanceReducer", "collection"));

    [Fact] Task should_report_diagnostic() => _result;
}

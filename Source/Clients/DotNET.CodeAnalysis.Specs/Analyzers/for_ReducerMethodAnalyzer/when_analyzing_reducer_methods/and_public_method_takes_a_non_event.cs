// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReducerMethodAnalyzer.when_analyzing_reducer_methods;

public class and_public_method_takes_a_non_event : given.a_reducer_method_analyzer
{
    Task _result;

    void Because()
    {
        var source = CreateSource("""
        #nullable enable
        public class SomeDto {}
        public class Model {}
        public class Reducer : Cratis.Chronicle.Reducers.IReducerFor<Model>
        {
            public Model Reduce({|#0:SomeDto dto|}, Model? current) => current ?? new Model();
        }
        """);
        var expected = new ExpectedDiagnostic(DiagnosticIds.ReducerEventParameterMustHaveAttribute, DiagnosticSeverity.Error, "SomeDto", "Reduce");
        _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerMethodAnalyzer>.VerifyAnalyzer(source, expected);
    }

    [Fact] Task should_report_the_non_dispatchable_public_method() => _result;
}

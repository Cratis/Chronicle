// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ConstraintImperativeCodeAnalyzer.when_analyzing_constraint_define_methods;

public class and_define_only_contains_builder_calls : given.a_constraint_imperative_code_analyzer
{
    const string Usage = """
    public class MyConstraint : Cratis.Chronicle.Events.Constraints.IConstraint
    {
        public void Define(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder)
        {
            builder.PerEventSourceType();
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ConstraintImperativeCodeAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostics() => _result;
}

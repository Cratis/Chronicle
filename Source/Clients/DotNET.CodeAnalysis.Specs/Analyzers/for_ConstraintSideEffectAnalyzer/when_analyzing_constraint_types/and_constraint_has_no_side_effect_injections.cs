// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ConstraintSideEffectAnalyzer.when_analyzing_constraint_types;

public class and_constraint_has_no_side_effect_injections : given.a_constraint_side_effect_analyzer
{
    const string Usage = """
    public interface ISomeService { }

    public class MyConstraint : Cratis.Chronicle.Events.Constraints.IConstraint
    {
        public MyConstraint(ISomeService service) { }
        public void Define(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder) { }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ConstraintSideEffectAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostics() => _result;
}

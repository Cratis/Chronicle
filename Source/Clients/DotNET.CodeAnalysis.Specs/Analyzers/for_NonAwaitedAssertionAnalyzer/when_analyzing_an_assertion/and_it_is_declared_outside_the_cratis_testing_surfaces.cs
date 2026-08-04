// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_NonAwaitedAssertionAnalyzer.when_analyzing_an_assertion;

/// <summary>
/// The pin that keeps the namespace gate a gate: an unrelated Should-prefixed API is not an assertion this rule
/// knows anything about, and reaching it would turn a targeted rule into noise on a consumer's own surface.
/// </summary>
public class and_it_is_declared_outside_the_cratis_testing_surfaces : given.a_non_awaited_assertion_analyzer
{
    const string Usage = """
    public class Spec
    {
        IOrder _order;

        void should_have_been_placed() => _order.ShouldHaveBeenPlaced();
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.NonAwaitedAssertionAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostics() => _result;
}

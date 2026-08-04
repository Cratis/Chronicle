// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_NonAwaitedAssertionAnalyzer.when_analyzing_an_assertion;

/// <summary>
/// The kernel-backed integration assertions are a Cratis testing surface that does not sit under a '.Testing.'
/// namespace, so a consumer adopting that tier gets assertions the rule has to police just the same.
/// </summary>
public class and_it_is_declared_in_the_integration_testing_surface : given.a_non_awaited_assertion_analyzer
{
    const string Usage = """
    public class Spec
    {
        IChronicleSetupFixture _fixture;

        void should_have_the_expected_tail() =>
            {|#0:_fixture.ShouldHaveTailSequenceNumber(1)|};
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.NonAwaitedAssertionAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.NonAwaitedAssertion, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning));

    [Fact] Task should_report_the_discarded_assertion() => _result;
}

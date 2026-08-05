// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_NonAwaitedAssertionAnalyzer.when_analyzing_an_assertion;

/// <summary>
/// The generic ValueTask shape closes the last of the four awaitables an assertion can be declared to return.
/// </summary>
public class and_it_returns_a_generic_value_task : given.a_non_awaited_assertion_analyzer
{
    const string Usage = """
    public class Spec
    {
        IEventSequence _eventLog;

        void should_have_the_expected_next_sequence_number()
        {
            {|#0:_eventLog.ShouldHaveNextSequenceNumber()|};
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.NonAwaitedAssertionAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.NonAwaitedAssertion, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning));

    [Fact] Task should_report_the_discarded_assertion() => _result;
}

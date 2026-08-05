// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_NonAwaitedAssertionAnalyzer.when_analyzing_an_assertion;

/// <summary>
/// The rule dispatches on the resolved method symbol, so an instance call and a plain static call are reported
/// alongside the reduced extension call. This pins behavior that already holds rather than adding any.
/// </summary>
public class and_it_is_not_an_extension_method : given.a_non_awaited_assertion_analyzer
{
    const string Usage = """
    public class Spec
    {
        IEventSequence _eventLog;
        EventSequenceAssertions _assertions;

        void should_have_been_appended_to()
        {
            {|#0:_assertions.ShouldHaveBeenAppendedTo()|};
        }

        void should_have_been_started()
        {
            {|#1:EventSequenceAssertions.ShouldHaveBeenStarted(_eventLog)|};
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.NonAwaitedAssertionAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.NonAwaitedAssertion, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning),
        new ExpectedDiagnostic(DiagnosticIds.NonAwaitedAssertion, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning));

    [Fact] Task should_report_both_discarded_assertions() => _result;
}

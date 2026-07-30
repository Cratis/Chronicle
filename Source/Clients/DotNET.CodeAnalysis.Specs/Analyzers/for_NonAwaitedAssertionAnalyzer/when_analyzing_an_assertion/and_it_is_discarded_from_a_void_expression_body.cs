// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_NonAwaitedAssertionAnalyzer.when_analyzing_an_assertion;

/// <summary>
/// The reported shape — a void-bodied fact. CS4014 stays silent because the member is not async, so nothing
/// but this analyzer tells the author the assertion can never fail.
/// </summary>
public class and_it_is_discarded_from_a_void_expression_body : given.a_non_awaited_assertion_analyzer
{
    const string Usage = """
    public class AnEvent { }

    public class Spec
    {
        IEventSequence _eventLog;

        void should_have_appended_the_event() =>
            {|#0:_eventLog.ShouldHaveAppendedEvent<AnEvent>()|};
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.NonAwaitedAssertionAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.NonAwaitedAssertion, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning));

    [Fact] Task should_report_the_discarded_assertion() => _result;
}

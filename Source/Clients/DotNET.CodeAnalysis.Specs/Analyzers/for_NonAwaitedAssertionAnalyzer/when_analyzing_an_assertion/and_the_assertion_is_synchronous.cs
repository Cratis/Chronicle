// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_NonAwaitedAssertionAnalyzer.when_analyzing_an_assertion;

/// <summary>
/// A void-returning assertion throws on the calling thread, so discarding "the result" is exactly how it is
/// meant to be used. Flagging it would fire on most of the assertion surface.
/// </summary>
public class and_the_assertion_is_synchronous : given.a_non_awaited_assertion_analyzer
{
    const string Usage = """
    public class Spec
    {
        IEventSequence _eventLog;

        void should_be_successful() => _eventLog.ShouldBeSuccessful();
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.NonAwaitedAssertionAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostics() => _result;
}

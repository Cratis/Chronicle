// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_NonAwaitedAssertionAnalyzer.when_analyzing_an_assertion;

/// <summary>
/// Widening the return-type gate must not cost the definition of discarded — an awaited assertion observes its
/// exception whichever awaitable it hands back.
/// </summary>
public class and_the_wider_awaitables_are_awaited : given.a_non_awaited_assertion_analyzer
{
    const string Usage = """
    public class Spec
    {
        IEventSequence _eventLog;

        async Task should_observe_every_awaitable()
        {
            await _eventLog.ShouldHaveAppendedEvents();
            await _eventLog.ShouldHaveNoEvents();
            await _eventLog.ShouldHaveNextSequenceNumber();
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.NonAwaitedAssertionAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostics() => _result;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_NonAwaitedAssertionAnalyzer.when_analyzing_an_assertion;

/// <summary>
/// Handing the Task back to the test runner observes it just as awaiting does — this is the shape the existing
/// analyzer specs themselves use, and it must not be flagged.
/// </summary>
public class and_the_task_is_returned_to_the_caller : given.a_non_awaited_assertion_analyzer
{
    const string Usage = """
    public class AnEvent { }

    public class Spec
    {
        IEventSequence _eventLog;

        Task should_have_appended_the_event() => _eventLog.ShouldHaveAppendedEvent<AnEvent>();

        Task should_have_the_expected_tail()
        {
            return _eventLog.ShouldHaveTailSequenceNumber(1);
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.NonAwaitedAssertionAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostics() => _result;
}

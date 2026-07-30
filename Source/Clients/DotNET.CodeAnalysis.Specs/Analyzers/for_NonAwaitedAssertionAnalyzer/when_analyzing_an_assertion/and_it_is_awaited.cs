// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_NonAwaitedAssertionAnalyzer.when_analyzing_an_assertion;

public class and_it_is_awaited : given.a_non_awaited_assertion_analyzer
{
    const string Usage = """
    public class AnEvent { }

    public class Spec
    {
        IEventSequence _eventLog;

        async Task should_have_appended_the_event() =>
            await _eventLog.ShouldHaveAppendedEvent<AnEvent>();
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.NonAwaitedAssertionAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostics() => _result;
}

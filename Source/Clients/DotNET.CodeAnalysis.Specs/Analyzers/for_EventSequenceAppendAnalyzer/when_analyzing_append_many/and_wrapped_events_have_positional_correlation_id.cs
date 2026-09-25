// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventSequenceAppendAnalyzer.when_analyzing_append_many;

public class and_wrapped_events_have_positional_correlation_id : given.an_event_sequence_append_analyzer
{
    const string Usage = """
    public class Usage
    {
        public void Append()
        {
            var sequence = new Cratis.Chronicle.EventSequences.EventSequence();
            var events = new[] { new Cratis.Chronicle.EventSequences.EventForEventSourceId() };
            sequence.AppendMany(events, "correlation");
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.EventSequenceAppendAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_a_diagnostic_for_the_correlation_id() => _result;
}

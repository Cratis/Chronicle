// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_EventSequenceAppendAnalyzer.when_analyzing_append;

public class and_type_is_marked_with_event_type_generation_for : given.an_event_sequence_append_analyzer
{
    const string Usage = """
    [EventType]
    public record KnownEvent;

    [Cratis.Chronicle.Events.EventTypeGenerationFor<KnownEvent>(1)]
    public record PreviousEvent;

    public class Usage
    {
        public void Append()
        {
            var sequence = new Cratis.Chronicle.EventSequences.EventSequence();
            sequence.Append("source", new PreviousEvent());
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.EventSequenceAppendAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_diagnostics() => _result;
}

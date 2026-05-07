// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorEventStoreAndEventSequenceAnalyzer.when_analyzing_reactor_types;

public class and_only_event_store_is_set : given.a_reactor_event_store_and_event_sequence_analyzer
{
    const string Usage = """
    [EventStore("external-store")]
    [Reactor]
    public class ValidReactor : IReactor
    {
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorEventStoreAndEventSequenceAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostics() => _result;
}
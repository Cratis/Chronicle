// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_InertEventFilterOnProjectionAnalyzer.when_analyzing_an_observer;

/// <summary>
/// A reactor really does filter on this metadata - its definition carries the filters and the subscription passes
/// them - so the same attribute is doing its job here and must stay unreported.
/// </summary>
public class and_it_is_a_reactor : given.an_inert_event_filter_analyzer
{
    const string Usage = """
    [EventStreamType("the-stream")]
    [FilterEventsByTag("the-tag")]
    public class TheReactor : Cratis.Chronicle.Reactors.IReactor
    {
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.InertEventFilterOnProjectionAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_report_nothing() => _result;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_SinkAndPassiveCombinedAnalyzer.when_analyzing_a_type;

public class and_it_is_only_marked_sink : given.a_sink_and_passive_combined_analyzer
{
    const string Usage = """
    [Sink("reporting")]
    public record OrderSummary(string Id);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.SinkAndPassiveCombinedAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}

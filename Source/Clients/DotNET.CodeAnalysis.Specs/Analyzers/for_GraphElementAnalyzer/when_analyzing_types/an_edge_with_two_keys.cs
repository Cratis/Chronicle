// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_GraphElementAnalyzer.when_analyzing_types;

public class an_edge_with_two_keys : given.a_graph_element_analyzer
{
    const string Usage = """
    [Edge("MEMBER_OF")] public record {|#0:MemberOf|}([Key] Guid MembershipId, [Key] Guid OtherId);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.GraphElementAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.GraphElementKeyMustBeDeclaredOnce, DiagnosticSeverity.Error, "edge", "MemberOf", "2", "but an edge can have at most one"));

    [Fact] Task should_report_the_extra_key() => _result;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_GraphElementAnalyzer.when_analyzing_types;

public class an_edge_with_a_list_of_objects : given.a_graph_element_analyzer
{
    const string Usage = """
    public record Visit(string Place);
    [Edge("VISITED")] public record Visited(Guid PersonId, Guid PlaceId, List<Visit> {|#0:Visits|});
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.GraphElementAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.GraphElementPropertyMustBePrimitive, DiagnosticSeverity.Error, "Visits", "edge", "Visited"));

    [Fact] Task should_report_the_nested_object() => _result;
}

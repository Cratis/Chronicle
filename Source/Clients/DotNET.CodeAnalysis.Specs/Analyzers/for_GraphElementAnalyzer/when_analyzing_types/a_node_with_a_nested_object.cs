// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_GraphElementAnalyzer.when_analyzing_types;

public class a_node_with_a_nested_object : given.a_graph_element_analyzer
{
    const string Usage = """
    public record Address(string Street);
    [Node] public record Person([Key] Guid PersonId, Address {|#0:Home|});
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.GraphElementAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.GraphElementPropertyMustBePrimitive, DiagnosticSeverity.Error, "Home", "node", "Person"));

    [Fact] Task should_report_the_nested_object() => _result;
}

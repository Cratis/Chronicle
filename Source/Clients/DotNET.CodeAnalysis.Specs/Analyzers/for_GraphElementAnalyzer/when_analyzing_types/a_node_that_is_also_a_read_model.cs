// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_GraphElementAnalyzer.when_analyzing_types;

public class a_node_that_is_also_a_read_model : given.a_graph_element_analyzer
{
    const string Usage = """
    [Node] [ReadModel] public record {|#0:Person|}([Key] Guid PersonId);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.GraphElementAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.GraphElementCannotBeReadModel, DiagnosticSeverity.Error, "Person"));

    [Fact] Task should_report_that_it_cannot_be_both() => _result;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_GraphViewAnalyzer.when_analyzing_types;

public class a_view_that_is_also_a_node : given.a_graph_view_analyzer
{
    const string Usage = """
    [ViewOf<Person>] [Node] public record {|#0:PersonSummary|}([Key] Guid Id);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.GraphViewAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.GraphViewCannotBeNodeOrEdge, DiagnosticSeverity.Error, "PersonSummary"));

    [Fact] Task should_report_the_view() => _result;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_GraphViewAnalyzer.when_analyzing_types;

public class a_view_anchored_at_a_type_that_is_not_a_node : given.a_graph_view_analyzer
{
    const string Usage = """
    public record Address(string Street);
    [ViewOf<Address>] public record {|#0:AddressView|}(string Street);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.GraphViewAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.GraphViewRootMustBeNode, DiagnosticSeverity.Error, "AddressView", "Address"));

    [Fact] Task should_report_the_root() => _result;
}

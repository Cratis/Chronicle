// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_AmbiguousChildrenParentKeyAnalyzer.when_analyzing_children_from;

public class and_parent_key_is_ambiguous : given.an_ambiguous_children_parent_key_analyzer
{
    const string Usage = """
    public record Recorded(Guid AccountId, Guid CustomerId, Guid Ticket);

    public record Line(Guid Ticket);

    public record Ledger(
        Guid Id,
        {|#0:[ChildrenFrom<Recorded>(key: "Ticket")] IEnumerable<Line> Items|});
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.AmbiguousChildrenParentKeyAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.AmbiguousChildrenParentKey, DiagnosticSeverity.Warning, "Recorded", "Guid"));

    [Fact] Task should_report_the_ambiguous_parent_key_diagnostic() => _result;
}

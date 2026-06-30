// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_AmbiguousChildrenParentKeyAnalyzer.when_analyzing_children_from;

public class and_only_one_other_property_matches : given.an_ambiguous_children_parent_key_analyzer
{
    const string Usage = """
    public record Recorded(Guid AccountId, Guid Ticket);

    public record Line(Guid Ticket);

    public record Ledger(
        Guid Id,
        [ChildrenFrom<Recorded>(key: "Ticket")] IEnumerable<Line> Items);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.AmbiguousChildrenParentKeyAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}

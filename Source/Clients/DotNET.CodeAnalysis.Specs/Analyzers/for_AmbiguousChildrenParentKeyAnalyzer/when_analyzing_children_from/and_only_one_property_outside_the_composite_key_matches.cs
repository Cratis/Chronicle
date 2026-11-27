// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_AmbiguousChildrenParentKeyAnalyzer.when_analyzing_children_from;

public class and_only_one_property_outside_the_composite_key_matches : given.an_ambiguous_children_parent_key_analyzer
{
    const string Usage = """
    public record Reference(Guid Value);
    public record Recorded(Reference Site, Reference Item, Reference Parent);
    public record Line(Guid Id);

    public record Ledger(
        Reference Id,
        [ChildrenFrom<Recorded>(CompositeKey = new[] { "site.Value", nameof(Recorded.Item) })] IEnumerable<Line> Items);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.AmbiguousChildrenParentKeyAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}

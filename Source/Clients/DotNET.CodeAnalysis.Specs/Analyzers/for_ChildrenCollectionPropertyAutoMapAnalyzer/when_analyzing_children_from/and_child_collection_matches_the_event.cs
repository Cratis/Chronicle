// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ChildrenCollectionPropertyAutoMapAnalyzer.when_analyzing_children_from;

public class and_child_collection_matches_the_event : given.a_children_collection_property_auto_map_analyzer
{
    const string Usage = """
    public record Note(string Text);

    public record LineAdded(string LineNumber, string Description, IReadOnlyList<Note> Notes);

    public record Line(
        [Key] string LineNumber,
        string Description,
        IReadOnlyList<Note> Notes);

    public record Order(
        Guid Id,
        [ChildrenFrom<LineAdded>(key: "LineNumber")] IEnumerable<Line> Lines);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ChildrenCollectionPropertyAutoMapAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}

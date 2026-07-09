// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ChildrenCollectionPropertyAutoMapAnalyzer.when_analyzing_children_from;

public class and_child_collection_does_not_match_the_event : given.a_children_collection_property_auto_map_analyzer
{
    const string Usage = """
    public record Note(string Text);

    public record LineAdded(string LineNumber, string Description, IReadOnlyList<Note> Annotations);

    public record Line(
        [Key] string LineNumber,
        string Description,
        IReadOnlyList<Note> Notes);

    public record Order(
        Guid Id,
        {|#0:[ChildrenFrom<LineAdded>(key: "LineNumber")] IEnumerable<Line> Lines|});
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ChildrenCollectionPropertyAutoMapAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ChildrenCollectionPropertyAutoMapsToNothing, DiagnosticSeverity.Warning, "Notes", "LineAdded"));

    [Fact] Task should_report_the_unmapped_collection_diagnostic() => _result;
}

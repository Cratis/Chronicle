// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReadModelPropertyMustHaveMappingSourceAnalyzer.when_analyzing;

/// <summary>
/// The events that populate a child record are named by the parent's [ChildrenFrom], never by the child itself, so a
/// child judged only on its own attributes reads as entirely unmapped when it is nothing of the sort. Here Quantity
/// is carried by the parent's child event and by no event the child names for itself.
/// </summary>
public class and_property_is_on_a_child_record : given.a_read_model_property_must_have_mapping_source_analyzer
{
    const string Usage = """
    public record OrderPlaced(string Reference);

    public record LineAdded(Guid LineId, string Quantity);

    public record LineRenamed(Guid LineId, string NewDescription);

    public record Line(Guid Id, [SetFrom<LineRenamed>("NewDescription")] string Description, string Quantity);

    [FromEvent<OrderPlaced>]
    public record Order(Guid Id, string Reference, [ChildrenFrom<LineAdded>] IReadOnlyList<Line> Lines);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReadModelPropertyMustHaveMappingSourceAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}

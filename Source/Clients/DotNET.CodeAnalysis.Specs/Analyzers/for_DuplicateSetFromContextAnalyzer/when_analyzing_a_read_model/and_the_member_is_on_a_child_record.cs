// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_DuplicateSetFromContextAnalyzer.when_analyzing_a_read_model;

/// <summary>
/// The builder maps a child record's parameters through a third copy of the same loop, so a check that only
/// looked at root read models would leave children unguarded - which is where the duplicate is least visible.
/// </summary>
public class and_the_member_is_on_a_child_record : given.a_duplicate_set_from_context_analyzer
{
    const string Usage = """
    [Cratis.Chronicle.Concepts.Events.EventTypeAttribute]
    public class LineAdded
    {
    }

    public record Line(
        Guid LineId,

        [SetFromContext<LineAdded>("SequenceNumber")]
        [SetFromContext<LineAdded>("Occurred")]
        {|#0:DateTimeOffset Stamp|});

    public record Basket(
        Guid Id,

        [ChildrenFrom<LineAdded>("LineId")]
        IReadOnlyList<Line> Lines);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.DuplicateSetFromContextAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.DuplicateSetFromContextForSameEventType, DiagnosticSeverity.Warning, "Stamp", "LineAdded"));

    [Fact] Task should_report_the_discarded_mapping_on_the_child() => _result;
}

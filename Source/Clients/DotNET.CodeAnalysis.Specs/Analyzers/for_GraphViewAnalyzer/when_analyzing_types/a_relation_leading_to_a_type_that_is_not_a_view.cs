// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_GraphViewAnalyzer.when_analyzing_types;

public class a_relation_leading_to_a_type_that_is_not_a_view : given.a_graph_view_analyzer
{
    const string Usage = """
    [ViewOf<Person>] public record PersonWithGroups(Guid Id, [Follows<MemberOf>] IReadOnlyList<Group> {|#0:Groups|});
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.GraphViewAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.GraphViewRelationTargetMustBeView, DiagnosticSeverity.Error, "Groups", "PersonWithGroups", "Group"));

    [Fact] Task should_report_the_target() => _result;
}

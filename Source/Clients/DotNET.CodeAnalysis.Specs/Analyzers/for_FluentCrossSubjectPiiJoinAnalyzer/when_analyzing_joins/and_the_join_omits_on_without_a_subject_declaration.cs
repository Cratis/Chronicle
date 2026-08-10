// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentCrossSubjectPiiJoinAnalyzer.when_analyzing_joins;

/// <summary>
/// A child Join validly omits On and its callback, but IdentifiedBy does not prove persisted runtime-subject equality.
/// </summary>
public class and_the_join_omits_on_without_a_subject_declaration : given.a_fluent_cross_subject_pii_join_analyzer
{
    const string Usage = """
    [PII]
    public record DisplayName(string Value);

    public record AdvisorNamed(DisplayName DisplayName);

    public record AdvisorSummary(
        [Key] Guid AdvisorId,
        DisplayName DisplayName);

    public record AdvisorBook([Key] Guid Id, IEnumerable<AdvisorSummary> Advisors);

    public class AdvisorBookProjection : IProjectionFor<AdvisorBook>
    {
        public void Define(IProjectionBuilderFor<AdvisorBook> builder) => builder
            .Children(m => m.Advisors, children => children
                .IdentifiedBy(m => m.AdvisorId)
                .{|#0:Join<AdvisorNamed>|}());
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentCrossSubjectPiiJoinAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.UnprovableCrossSubjectPiiJoin, DiagnosticSeverity.Warning, "DisplayName", "AdvisorNamed", "Id"));

    [Fact] Task should_report_the_unprovable_subject_warning() => _result;
}
